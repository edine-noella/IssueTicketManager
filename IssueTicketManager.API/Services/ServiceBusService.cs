using System.Text.Json;
using Azure.Messaging.ServiceBus;
using IssueTicketManager.API.Configuration;
using IssueTicketManager.API.Messages;
using IssueTicketManager.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace IssueTicketManager.API.Services
{
    public class ServiceBusService : IServiceBusService, IAsyncDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusConfiguration _configuration;
        private readonly ILogger<ServiceBusService> _logger;
        private readonly Dictionary<string, ServiceBusSender> _senders = new();

        public ServiceBusService(
            ServiceBusClient client,
            IOptions<ServiceBusConfiguration> configuration,
            ILogger<ServiceBusService> logger)
        {
            _client = client;
            _configuration = configuration.Value;
            _logger = logger;
        }

        public async Task PublishAsync<T>(T message, string topicName, CancellationToken cancellationToken = default) where T : BaseMessage
        {
            try
            {
                var sender = await GetOrCreateSenderAsync(topicName);
                var json = JsonSerializer.Serialize(message);
                var serviceBusMessage = new ServiceBusMessage(json)
                {
                    ContentType = "application/json",
                    CorrelationId = message.CorrelationId,
                    Subject = message.EventType
                };

                // Add custom properties for filtering
                serviceBusMessage.ApplicationProperties["EventType"] = message.EventType;
                serviceBusMessage.ApplicationProperties["Timestamp"] = message.Timestamp.ToString("O");

                await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
                
                _logger.LogInformation("Successfully published message of type {MessageType} to topic {TopicName} with correlation ID {CorrelationId}", 
                    typeof(T).Name, topicName, message.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message of type {MessageType} to topic {TopicName}", 
                    typeof(T).Name, topicName);
                throw;
            }
        }

        public async Task PublishUserCreatedAsync(UserCreatedMessage message, CancellationToken cancellationToken = default)
        {
            await PublishAsync(message, _configuration.Topics.UserCreate, cancellationToken);
        }

        public async Task PublishLabelCreatedAsync(LabelCreatedMessage message, CancellationToken cancellationToken = default)
        {
            await PublishAsync(message, _configuration.Topics.LabelCreate, cancellationToken);
        }

        public async Task PublishIssueCreatedAsync(IssueCreatedMessage message, CancellationToken cancellationToken = default)
        {
            await PublishAsync(message, _configuration.Topics.IssueCreate, cancellationToken);
        }

        public async Task PublishIssueUpdatedAsync(IssueUpdatedMessage message, CancellationToken cancellationToken = default)
        {
            await PublishAsync(message, _configuration.Topics.IssueUpdate, cancellationToken);
        }

        public async Task PublishIssueAssignedAsync(IssueAssignedMessage message, CancellationToken cancellationToken = default)
        {
            await PublishAsync(message, _configuration.Topics.IssueUserAssign, cancellationToken);
        }

        public async Task PublishIssueCommentCreatedAsync(IssueCommentCreatedMessage message, CancellationToken cancellationToken = default)
        {
            await PublishAsync(message, _configuration.Topics.IssueCommentCreate, cancellationToken);
        }

        public async Task PublishIssueLabelAssignedAsync(IssueLabelAssignedMessage message, CancellationToken cancellationToken = default)
        {
            await PublishAsync(message, _configuration.Topics.IssueLabelAssign, cancellationToken);
        }

        private async Task<ServiceBusSender> GetOrCreateSenderAsync(string topicName)
        {
            if (_senders.TryGetValue(topicName, out var existingSender) && !_client.IsClosed)
            {
                return existingSender;
            }
            
            _logger.LogWarning("Sender for topic {topicName} was disposed or closed; recreating sender.", topicName);
            return await CreateAndCacheSenderAsync(topicName);
          
        }

        private Task<ServiceBusSender> CreateAndCacheSenderAsync(string topicName)
        {
            var sender = _client.CreateSender(topicName);
            _senders[topicName] = sender;
            return Task.FromResult(sender);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var sender in _senders.Values)
            {
                await sender.DisposeAsync();
            }
            _senders.Clear();
            // await _client.DisposeAsync();
        }
    }
}