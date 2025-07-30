using Azure.Messaging.ServiceBus;

namespace IssueTicketManager.API.Services.Interfaces;

public interface IMessageProcessor
{
    Task HandleMessageAsync(ProcessMessageEventArgs args);
    Task HandleErrorAsync(ProcessErrorEventArgs args);
}