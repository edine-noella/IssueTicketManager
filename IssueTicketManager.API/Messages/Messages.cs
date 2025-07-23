using System.Text.Json.Serialization;

namespace IssueTicketManager.API.Messages
{
    public abstract class BaseMessage
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    }

    public class UserCreatedMessage : BaseMessage
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        public UserCreatedMessage()
        {
            EventType = "user.create";
        }
    }

    public class LabelCreatedMessage : BaseMessage
    {
        public int LabelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        
        public LabelCreatedMessage()
        {
            EventType = "label.create";
        }
    }

    public class IssueCreatedMessage : BaseMessage
    {
        public int IssueId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int CreatorId { get; set; }
        public List<int> LabelIds { get; set; } = new();
        
        public IssueCreatedMessage()
        {
            EventType = "issue.create";
        }
    }

    public class IssueUpdatedMessage : BaseMessage
    {
        public int IssueId { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Status { get; set; }
        public int? AssigneeId { get; set; }
        public List<int>? LabelIds { get; set; }
        
        public IssueUpdatedMessage()
        {
            EventType = "issue.update";
        }
    }

    public class IssueAssignedMessage : BaseMessage
    {
        public int IssueId { get; set; }
        public int AssigneeId { get; set; }
        public int AssignedByUserId { get; set; }
        
        public IssueAssignedMessage()
        {
            EventType = "issue.user.assign";
        }
    }

    public class IssueCommentCreatedMessage : BaseMessage
    {
        public int CommentId { get; set; }
        public int IssueId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        
        public IssueCommentCreatedMessage()
        {
            EventType = "issue.comment.create";
        }
    }

    public class IssueLabelAssignedMessage : BaseMessage
    {
        public int IssueId { get; set; }
        public int LabelId { get; set; }
        public int AssignedByUserId { get; set; }
        
        public IssueLabelAssignedMessage()
        {
            EventType = "issue.label.assign";
        }
    }
}