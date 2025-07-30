using Azure.Messaging.ServiceBus;
using IssueTicketManager.API.Configuration;
using IssueTicketManager.API.Services;
using Microsoft.Extensions.Options;

public class ServiceBusBackgroundService : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider; 
    private readonly ILogger<ServiceBusBackgroundService> _logger;
    private readonly List<ServiceBusProcessor> _processors = new();

    public ServiceBusBackgroundService(
        ServiceBusClient client,
        IOptions<ServiceBusConfiguration> config,
        IServiceProvider serviceProvider, 
        ILogger<ServiceBusBackgroundService> logger)
    {
        _client = client;
        _configuration = config.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topics = new List<string>
        {
            _configuration.Topics.UserCreate,
            _configuration.Topics.LabelCreate,
            _configuration.Topics.IssueCreate,
            _configuration.Topics.IssueUpdate,
            _configuration.Topics.IssueUserAssign,
            _configuration.Topics.IssueCommentCreate,
            _configuration.Topics.IssueLabelAssign
        };

        foreach (var topic in topics)
        {
            var processor = _client.CreateProcessor(topic, "import", new ServiceBusProcessorOptions());

            // Create handler delegate which creates a scope per message
            processor.ProcessMessageAsync += async args =>
            {
                using var scope = _serviceProvider.CreateScope();

                var handler = scope.ServiceProvider.GetRequiredService<MessageHandlerService>();
                await handler.HandleMessageAsync(args);
            };

            processor.ProcessErrorAsync += async args =>
                _logger.LogError(args.Exception, "Error processing message from topic {Topic}", topic);

            _processors.Add(processor);
            await processor.StartProcessingAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
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
