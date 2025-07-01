using System.ComponentModel.DataAnnotations;

namespace IssueTicketManager.API.DTOs;

public class AssignUserIssueDto
{
    [Required]
    public int? AssigneeId { get; set; }
}