using IssueTicketManager.API.Data;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace IssueTicketManager.API.Repositories;

public class IssueRepository : IIssueRepository
{
    private readonly ApplicationDbContext _context;

    public IssueRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Issue> CreateIssueAsync(Issue issue)
    {
        issue.CreatedAt = DateTime.UtcNow;
        issue.UpdatedAt = DateTime.UtcNow;
        issue.Status = IssueStatus.New;

        await _context.Issues.AddAsync(issue);
        await _context.SaveChangesAsync();
        return issue;
    }

    public async Task<Issue> UpdateIssueAsync(Issue issue)
    {
        issue.UpdatedAt = DateTime.UtcNow;
        
         _context.Issues.Update(issue);
        await _context.SaveChangesAsync();
        return issue;
    }

    public async Task<Issue?> GetIssueByIdAsync(int id)
    {
        return await _context.Issues
            .Include(i => i.Creator)
            .Include(i => i.Assignee)
            .Include(i => i.IssueLabels)
            .ThenInclude(il => il.Label)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Issue>> GetAllIssuesAsync()
    {
        return await _context.Issues
            .Include(i => i.Creator)
            .Include(i => i.Assignee)
            .Include(i => i.IssueLabels)
            .ThenInclude(il => il.Label)
            .ToListAsync();
    }

    public async Task<bool> IssueExistsAsync(int id)
    {
        return await _context.Issues.AnyAsync(i => i.Id == id);
    }

    public async Task<(Issue?, LabelAddResult)> AddLabelToIssueAsync(int issueId, int labelId)
    {
        var issueExists = await _context.Issues
            .Include(i => i.IssueLabels)
            .ThenInclude(il => il.Label)
            .FirstOrDefaultAsync(i => i.Id == issueId);
        if(issueExists == null) return (null, LabelAddResult.IssueNotFound);
        
        var labelExists = await _context.Labels.AnyAsync(l => l.Id == labelId);
        if (!labelExists) return (null, LabelAddResult.LabelNotFound);
        
        var alreadyAssigned = await _context.IssueLabels.AnyAsync(il => il.IssueId == issueId && il.LabelId == labelId);
        if (alreadyAssigned) return (issueExists, LabelAddResult.AlreadyAssigned);

        var issueLabel = new IssueLabel
        {
            IssueId = issueId,
            LabelId = labelId
        };
        
        await _context.IssueLabels.AddAsync(issueLabel);
        await _context.SaveChangesAsync();
        var updatedIssue = await _context.Issues
            .Include(i => i.IssueLabels)
            .ThenInclude(il => il.Label)
            .FirstOrDefaultAsync(i => i.Id == issueId);
        return (updatedIssue, LabelAddResult.Success);
    }
    
   
}