namespace IssueTicketManager.API.Configuration
{
    public class ServiceBusConfiguration
    {
        public const string SectionName = "ServiceBus";
        
        public string ConnectionString { get; set; } = string.Empty;
        public TopicConfiguration Topics { get; set; } = new();
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    }

    public class TopicConfiguration
    {
        public string UserCreate { get; set; } = "user.create";
        public string LabelCreate { get; set; } = "label.create";
        public string IssueCreate { get; set; } = "issue.create";
        public string IssueUpdate { get; set; } = "issue.update";
        public string IssueUserAssign { get; set; } = "issue.user.assign";
        public string IssueCommentCreate { get; set; } = "issue.comment.create";
        public string IssueLabelAssign { get; set; } = "issue.label.assign";
    }
}