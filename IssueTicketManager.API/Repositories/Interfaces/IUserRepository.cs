using IssueTicketManager.API.Models;

namespace IssueTicketManager.API.Repositories.Interfaces;

public interface IUserRepository
{
    Task CreateUser(User user);
    Task UpdateUser(User user);
    Task<List<User>> GetUsers();
    Task<User?> GetUserByEmail(string email);
}