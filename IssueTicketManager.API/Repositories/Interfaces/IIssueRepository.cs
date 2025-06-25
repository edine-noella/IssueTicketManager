using IssueTicketManager.API.Models;

namespace IssueTicketManager.API.Repositories.Interfaces;

public interface IIssueRepository
{
    Task<Issue> CreateIssueAsync(Issue issue);
    Task UpdateIssueAsync(Issue issue);
    Task<Issue?> GetIssueByIdAsync(int id);
    Task<IEnumerable<Issue>> GetAllIssuesAsync();
    Task<bool> IssueExistsAsync(int id);
}