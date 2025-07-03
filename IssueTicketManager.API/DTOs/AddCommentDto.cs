using System.ComponentModel.DataAnnotations;

namespace IssueTicketManager.API.DTOs;

public class AddCommentDto
{
    [Required, MaxLength(2000)]
    public string Text { get; set; }
    
    [Required, Range(1, int.MaxValue)]
    public int UserId { get; set; }
    
    [Required, Range(1, int.MaxValue)]
    public int IssueId { get; set; }
}