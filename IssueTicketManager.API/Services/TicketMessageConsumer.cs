using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using IssueTicketManager.API.Configuration;
using IssueTicketManager.API.Messages;
using Microsoft.Extensions.Options;

namespace IssueTicketManager.API.Services;

public class TicketMessageConsumer: IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<TicketMessageConsumer> _logger;
    private readonly ServiceBusConfiguration _config;
    private ServiceBusProcessor _commentProcessor;

    public TicketMessageConsumer(ServiceBusClient client, ILogger<TicketMessageConsumer> logger, IOptions<ServiceBusConfiguration> config )
    {
        _client = client;
        _logger = logger;
        _config = config.Value;
    }

    public async Task StartListening()
    {
        var subscriptionName = "test-comment-subscription";
        
        // 1. First ensure subscription exists (where this needs to run once)
        await EnsureSubscriptionExists(subscriptionName);

        _commentProcessor = _client.CreateProcessor(
            "issue.comment.create",
            subscriptionName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false // For retries
            });
        
        // Set up a processor for comment messages
        _commentProcessor.ProcessMessageAsync += HandleCommentMessage;
        _commentProcessor.ProcessErrorAsync += HandleError;
        
        await _commentProcessor.StartProcessingAsync();
    }

    private async Task EnsureSubscriptionExists(string subscriptionName)
    {
        var adminClient = new ServiceBusAdministrationClient(_config.ConnectionString);
        if (!await adminClient.SubscriptionExistsAsync(_config.Topics.IssueCommentCreate, subscriptionName))
        {
            await adminClient.CreateSubscriptionAsync(new CreateSubscriptionOptions(
                topicName: _config.Topics.IssueCommentCreate, subscriptionName: subscriptionName)
            {
                MaxDeliveryCount = _config.MaxRetryAttempts + 1, // For initial attempt + retries,
                DeadLetteringOnMessageExpiration = true,
                DefaultMessageTimeToLive = TimeSpan.FromDays(14)
            });
        }
    }

    private async Task HandleCommentMessage(ProcessMessageEventArgs args)
    {
        try
        {
            
            // 1. Get the message content
            var messageBody = args.Message.Body.ToString();
            var comment = JsonSerializer.Deserialize<IssueCommentCreatedMessage>(messageBody);

            if (comment?.Content?.Contains("FAIL") == true)
            {
                throw new InvalidOperationException("Simulated poison message");
            }
            // 2. Do something with it (this is the business logic)
            _logger.LogInformation($"Hello, New comment #{comment.CommentId} on issue {comment.IssueId}");
            
            // 3. Mark the message as completed
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception e)
        {
           _logger.LogError(e, "Failed to process comment (Attempt: {Attempt})", args.Message.DeliveryCount);
           // Dead-letter if max retries reached
           if (args.Message.DeliveryCount >= _config.MaxRetryAttempts + 1)
           {
               await args.DeadLetterMessageAsync(args.Message,
                   deadLetterReason: "Max retries exceeded",
                   deadLetterErrorDescription: $"Failed after{args.Message.DeliveryCount} attempts");
           }
           else
           {
               await args.AbandonMessageAsync(args.Message); // Make it available for retry
               await Task.Delay(_config.RetryDelay); // Wait before retry

           }
        }
    }

    public Task HandleError(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, 
            "Service Bus Error (Source: {ErrorSource}, Entity: {EntityPath})",
            args.ErrorSource,
            args.EntityPath);   
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_commentProcessor != null)
        {
            await _commentProcessor.StopProcessingAsync();
            await _commentProcessor.DisposeAsync();
        }
    }
}