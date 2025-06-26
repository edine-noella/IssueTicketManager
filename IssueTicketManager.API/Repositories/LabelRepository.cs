using IssueTicketManager.API.Data;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

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
    
    public async Task<Label?> GetLabelByIdAsync(int id)
    {
        return await _context.Labels.FindAsync(id);
    }

    public async Task<IEnumerable<Label>> GetAllLabelsAsync()
    {
        return await _context.Labels.ToListAsync();
    }
    
}