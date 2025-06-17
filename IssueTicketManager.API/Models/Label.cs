using System.ComponentModel.DataAnnotations;

namespace IssueTicketManager.API.Models;

public class Label
{
    public int Id { get; set; }
        
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }
        
    public string Color { get; set; }
        
    public ICollection<IssueLabel> IssueLabels { get; set; }
}