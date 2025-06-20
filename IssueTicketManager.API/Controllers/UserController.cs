using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace IssueTicketManager.API.Controllers;

[Route("[controller]")]
[ApiController]
public class UserController : ControllerBase
{
   private readonly IUserRepository _userRepository;

   public UserController(IUserRepository userRepository)
   {
      _userRepository = userRepository;
   }

   [HttpPost]
   public async Task<ActionResult<User>> CreateUser([FromBody] User user)
   {
      if (!ModelState.IsValid)
      {
         return BadRequest(ModelState);
      }

      await _userRepository.CreateUser(user);

      return Ok("User created successfully.");
      // return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, user);
   }

   [HttpPut("{id}")]
   public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)
   {
      Console.WriteLine("We are updating the user here!");
      if (!ModelState.IsValid)
      {
         return BadRequest(ModelState);
      }

      await _userRepository.UpdateUser(user, id);
      return Ok("User updated successfully.");
   }
}