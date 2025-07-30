namespace IssueTicketManager.API.Services.Interfaces;

public interface IServiceBusConsumerService
{
    Task StartProcessingAsync(CancellationToken cancellationToken = default);
    Task StopProcessingAsync(CancellationToken cancellationToken = default);
}