using Azure.Messaging.ServiceBus;
using IssueTicketManager.API.Configuration;
using IssueTicketManager.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace IssueTicketManager.API.Services;

public class ServiceBusBackgroundService: BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusConfiguration _configuration;
    private readonly IProcessorFactory _processorFactory;
    private readonly ILogger<ServiceBusBackgroundService> _logger;
    private readonly List<ServiceBusProcessor> _processors = new();

    public ServiceBusBackgroundService(ServiceBusClient client, IOptions<ServiceBusConfiguration> config, IProcessorFactory processorFactory, ILogger<ServiceBusBackgroundService>logger)
    {
        _client = client;
        _configuration = config.Value;
        _processorFactory = processorFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topicConfig = _configuration.Topics;
        
        // List of all topic names from configuration
        var topics = new List<string>
        {
            topicConfig.UserCreate,
            topicConfig.LabelCreate,
            topicConfig.IssueCreate,
            topicConfig.IssueUpdate,
            topicConfig.IssueUserAssign,
            topicConfig.IssueCommentCreate,
            topicConfig.IssueLabelAssign
        };

        foreach (var topic  in topics)
        {
            await SetupProcessorAsync(topic, "import", stoppingToken);
        }
        
        // Start the processors
        foreach (var processor in _processors)
        {
            await processor.StartProcessingAsync(stoppingToken);
        }
        
    }
    private async Task SetupProcessorAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        var processor = _client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions());
        var messageProcessor = _processorFactory.CreateMessageProcessor(topicName);
            
        processor.ProcessMessageAsync += messageProcessor.HandleMessageAsync;
        processor.ProcessErrorAsync += messageProcessor.HandleErrorAsync;
            
        _processors.Add(processor);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var processor in _processors)
        {
            await processor.StopProcessingAsync(cancellationToken);
            await processor.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}