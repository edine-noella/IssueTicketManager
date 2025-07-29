using IssueTicketManager.API.Services.Interfaces;

namespace IssueTicketManager.API.Services;

public class ServiceBusBackgroundService: BackgroundService
{
    private readonly TicketMessageConsumer _consumer;
    private readonly ILogger<ServiceBusBackgroundService> _logger;

    public ServiceBusBackgroundService(TicketMessageConsumer consumer, ILogger<ServiceBusBackgroundService> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service Bus background service starting");
        await _consumer.StartListening();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        _logger.LogInformation("Service Bus background service stopping");
    }
}