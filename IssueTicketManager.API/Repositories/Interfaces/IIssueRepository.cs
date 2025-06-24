using IssueTicketManager.API.Models;

namespace IssueTicketManager.API.Repositories.Interfaces;

public interface IIssueRepository
{
    Task<Issue> CreateIssueAsync(Issue issue);
    // Task<Issue?> UpdateIssueAsync(int id, Action<Issue> updateAction);
    Task UpdateIssueAsync(Issue issue);
    Task<Issue?> GetIssueByIdAsync(int id);
    Task<IEnumerable<Issue>> GetAllIssuesAsync();
    Task<bool> IssueExistsAsync(int id);
}