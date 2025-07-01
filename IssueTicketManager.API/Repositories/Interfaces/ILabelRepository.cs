using IssueTicketManager.API.Models;

namespace IssueTicketManager.API.Repositories.Interfaces;

public interface ILabelRepository
{
    Task<Label> CreateLabelAsync(Label label);
    Task<Label?> GetLabelByIdAsync(int id);
    Task<IEnumerable<Label>> GetAllLabelsAsync();
   }