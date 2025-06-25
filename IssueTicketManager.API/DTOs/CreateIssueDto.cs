using System.ComponentModel.DataAnnotations;

namespace IssueTicketManager.API.DTOs;

public class CreateIssueDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; }
    
    [Required]
    public string Body { get; set; }
    
    [Required, Range(1, int.MaxValue)]
    public int CreatorId { get; set; }
    
    public List<int> LabelIds { get; set; } = new();
}