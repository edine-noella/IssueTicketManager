using System.ComponentModel.DataAnnotations;

namespace IssueTicketManager.API.Models;
public enum IssueStatus
{
    New,
    ToDo,
    InProgress,
    Blocked,
    Done
}
public class Issue
{
    public int Id { get; set; }
        
    [Required]
    [MaxLength(200)]
    public string Title { get; set; }
        
    [Required]
    public string Body { get; set; }
        
    public IssueStatus Status { get; set; } = IssueStatus.New;
        
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
    // Foreign keys
    public int CreatorId { get; set; }
    public int? AssigneeId { get; set; }
        
    public User Creator { get; set; } //issue has one creator
    public User Assignee { get; set; }
    
    public ICollection<Comment> Comments { get; set; }
    public ICollection<IssueLabel> IssueLabels { get; set; } 
    
}