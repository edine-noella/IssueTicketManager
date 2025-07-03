using IssueTicketManager.API.Data;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IssueTicketManager.API.Repositories;

public class CommentRepository: ICommentRepository
{
    public readonly ApplicationDbContext _context;

    public CommentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Comment> AddCommentAsync(Comment comment)
    {
      await  _context.Comments.AddAsync(comment);
      await _context.SaveChangesAsync();
      return comment;
    }
    
    public async Task<Comment?> GetCommentWithDetailsAsync(int id)
    {
        return await _context.Comments
            .Include(c => c.User)
            .Include(c => c.Issue)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
    
    public async Task<IEnumerable<Comment>> GetAllCommentsAsync()
    {
        return await _context.Comments
            .Include(c => c.User)
            .Include(c => c.Issue)
            .ToListAsync();
    }
}