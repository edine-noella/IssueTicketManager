using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace IssueTicketManager.API.Models;

[Index(nameof(Email), IsUnique = true)]
public class User
{
    public int Id { get; set; }
        
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
        
    [Required]
    [EmailAddress]
    public string Email { get; set; }
        
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
    
    public ICollection<Issue>? CreatedIssues { get; set; } //issues created by the user
    public ICollection<Issue>? AssignedIssues { get; set; }
    public ICollection<Comment>? Comments { get; set; }
    
}