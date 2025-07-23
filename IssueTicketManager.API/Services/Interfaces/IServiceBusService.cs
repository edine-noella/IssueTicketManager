using IssueTicketManager.API.Messages;

namespace IssueTicketManager.API.Services.Interfaces
{
    public interface IServiceBusService
    {
        Task PublishAsync<T>(T message, string topicName, CancellationToken cancellationToken = default) where T : BaseMessage;
        Task PublishUserCreatedAsync(UserCreatedMessage message, CancellationToken cancellationToken = default);
        Task PublishLabelCreatedAsync(LabelCreatedMessage message, CancellationToken cancellationToken = default);
        Task PublishIssueCreatedAsync(IssueCreatedMessage message, CancellationToken cancellationToken = default);
        Task PublishIssueUpdatedAsync(IssueUpdatedMessage message, CancellationToken cancellationToken = default);
        Task PublishIssueAssignedAsync(IssueAssignedMessage message, CancellationToken cancellationToken = default);
        Task PublishIssueCommentCreatedAsync(IssueCommentCreatedMessage message, CancellationToken cancellationToken = default);
        Task PublishIssueLabelAssignedAsync(IssueLabelAssignedMessage message, CancellationToken cancellationToken = default);
    }
}