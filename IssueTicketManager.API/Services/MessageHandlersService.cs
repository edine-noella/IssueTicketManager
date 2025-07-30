using System.Text.Json;
using Azure.Messaging.ServiceBus;
using IssueTicketManager.API.Messages;
using IssueTicketManager.API.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace IssueTicketManager.API.Services
{
    public class MessageHandlerService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IIssueRepository _issueRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILabelRepository _labelRepository;
        private readonly ILogger<MessageHandlerService> _logger;

        public MessageHandlerService(
            ICommentRepository commentRepository,
            IIssueRepository issueRepository,
            IUserRepository userRepository,
            ILabelRepository labelRepository,
            ILogger<MessageHandlerService> logger)
        {
            _commentRepository = commentRepository;
            _issueRepository = issueRepository;
            _userRepository = userRepository;
            _labelRepository = labelRepository;
            _logger = logger;
        }

        public async Task HandleMessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                var message = args.Message;
                
                
                if (!message.ApplicationProperties.TryGetValue("EventType", out var eventTypeObj))
                {
                    _logger.LogWarning("Message missing EventType property");
                    await args.DeadLetterMessageAsync(message, "Missing EventType property");
                    return;
                }

                var eventType = eventTypeObj.ToString();
                var messageBody = message.Body.ToString();

                switch (eventType)
                {
                    case "user.create":
                        await HandleUserCreated(JsonSerializer.Deserialize<UserCreatedMessage>(messageBody));
                        break;
                        
                    case "label.create":
                        await HandleLabelCreated(JsonSerializer.Deserialize<LabelCreatedMessage>(messageBody));
                        break;
                        
                    case "issue.create":
                        await HandleIssueCreated(JsonSerializer.Deserialize<IssueCreatedMessage>(messageBody));
                        break;
                        
                    case "issue.update":
                        await HandleIssueUpdated(JsonSerializer.Deserialize<IssueUpdatedMessage>(messageBody));
                        break;
                        
                    case "issue.user.assign":
                        await HandleIssueAssigned(JsonSerializer.Deserialize<IssueAssignedMessage>(messageBody));
                        break;
                        
                    case "issue.comment.create":
                        await HandleCommentCreated(JsonSerializer.Deserialize<IssueCommentCreatedMessage>(messageBody));
                        break;
                        
                    case "issue.label.assign":
                        await HandleLabelAssigned(JsonSerializer.Deserialize<IssueLabelAssignedMessage>(messageBody));
                        break;
                        
                    default:
                        _logger.LogWarning("Unknown event type: {EventType}", eventType);
                        await args.DeadLetterMessageAsync(message, "Unknown event type");
                        return;
                }

                await args.CompleteMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message");
                await args.AbandonMessageAsync(args.Message);
            }
        }

        private async Task HandleUserCreated(UserCreatedMessage message)
        {
            _logger.LogInformation("Processing new user {UserId}", message.UserId);
        
        }

        private async Task HandleLabelCreated(LabelCreatedMessage message)
        {
            _logger.LogInformation("Processing new label {LabelId}", message.LabelId);
       
        }

        private async Task HandleIssueCreated(IssueCreatedMessage message)
        {
            _logger.LogInformation("Processing new issue {IssueId}", message.IssueId);
          
        }

        private async Task HandleIssueUpdated(IssueUpdatedMessage message)
        {
            _logger.LogInformation("Processing update for issue {IssueId}", message.IssueId);
           
        }

        private async Task HandleIssueAssigned(IssueAssignedMessage message)
        {
            _logger.LogInformation("Processing assignment for issue {IssueId}", message.IssueId);
            
        }

        private async Task HandleCommentCreated(IssueCommentCreatedMessage message)
        {
            _logger.LogInformation("Processing new comment {CommentId} for issue {IssueId}", 
                message.CommentId, message.IssueId);
        
        }

        private async Task HandleLabelAssigned(IssueLabelAssignedMessage message)
        {
            _logger.LogInformation("Processing label assignment for issue {IssueId}", message.IssueId);
          
        }
    }
}