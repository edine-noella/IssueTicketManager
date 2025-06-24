using IssueTicketManager.API.Data;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;

namespace IssueTicketManager.API.Repositories;

public class LabelRepository : ILabelRepository
{
    
    private readonly ApplicationDbContext _context;

    public LabelRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Label> CreateLabelAsync(Label label)
    {
        await _context.Labels.AddAsync(label);
        await _context.SaveChangesAsync();
        return label;
    }
    
}