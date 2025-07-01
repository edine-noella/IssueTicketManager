using IssueTicketManager.API.Models;

namespace IssueTicketManager.API.Repositories.Interfaces;

public interface IIssueRepository
{
    Task<Issue> CreateIssueAsync(Issue issue);
    Task<Issue> UpdateIssueAsync(Issue issue);
    Task<Issue?> GetIssueByIdAsync(int id);
    Task<IEnumerable<Issue>> GetAllIssuesAsync();
    Task<bool> IssueExistsAsync(int id);
    
    Task<LabelAddResult> AddLabelToIssueAsync(int issueId, int labelId);
}

public enum LabelAddResult
{
    Success,
    IssueNotFound,
    LabelNotFound, 
    AlreadyAssigned
}