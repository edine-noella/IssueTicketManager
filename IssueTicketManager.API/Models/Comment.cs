using System.ComponentModel.DataAnnotations;

namespace IssueTicketManager.API.Models;

public class Comment
{
    public int Id { get; set; }
        
    [Required]
    [MaxLength(2000)]
    public string Text { get; set; }
        
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
    // Foreign keys
    public int UserId { get; set; }
    public int IssueId { get; set; }

    public User User { get; set; }
    public Issue Issue { get; set; }
    
}