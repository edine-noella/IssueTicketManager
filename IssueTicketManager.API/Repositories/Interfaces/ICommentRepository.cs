using IssueTicketManager.API.Models;

namespace IssueTicketManager.API.Repositories.Interfaces;

public interface ICommentRepository
{
    Task<Comment> AddCommentAsync(Comment comment);
    Task<Comment?> GetCommentWithDetailsAsync(int id);
    Task<IEnumerable<Comment>> GetAllCommentsAsync();
}