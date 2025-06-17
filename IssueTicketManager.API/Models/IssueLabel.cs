namespace IssueTicketManager.API.Models;

public class IssueLabel
{
    public int IssueId { get; set; }
    public int LabelId { get; set; }
        
    // Navigation properties
    public Issue Issue { get; set; }
    public Label Label { get; set; }
}