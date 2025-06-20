using IssueTicketManager.API.Data;
using IssueTicketManager.API.Models;

namespace IssueTicketManager.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    
    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task CreateUser(User user)
    {
        await _context.Users.AddAsync(user);
    }
    
    public async Task UpdateUser(User user, int id)
    {
        var userToUpdate = await _context.Users.FindAsync(id);
        userToUpdate.Name = user.Name;
        userToUpdate.Email = user.Email;
        await _context.SaveChangesAsync();
    }
}