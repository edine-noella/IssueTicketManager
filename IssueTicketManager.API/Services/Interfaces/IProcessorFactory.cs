namespace IssueTicketManager.API.Services.Interfaces;

public interface IProcessorFactory
{
    IMessageProcessor CreateMessageProcessor(string topicName);
}