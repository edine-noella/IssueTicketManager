using System.ComponentModel.DataAnnotations;

namespace IssueTicketManager.API.DTOs;

public class CreateLabelDto
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }

    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", 
        ErrorMessage = "Color must be a valid hex color (e.g., #FF0000 or #F00)")]
    public string Color { get; set; }
}