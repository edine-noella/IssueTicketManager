using IssueTicketManager.API.Models;

namespace IssueTicketManager.API.Repositories;

public interface IUserRepository
{
    Task CreateUser(User user);
    Task UpdateUser(User user, int id);
}