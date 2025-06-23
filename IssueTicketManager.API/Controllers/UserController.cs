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

      var existingUserWithSameEmail = await _userRepository.GetUserByEmail(user.Email);
      if (existingUserWithSameEmail != null)
      {
         return Conflict(new{ message ="User with this email already exists." });
      }
      await _userRepository.CreateUser(user);

      
      return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, user);
   }

   [HttpPut("{id}")]
   public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)
   {
      if (!ModelState.IsValid)
      {
         return BadRequest(ModelState);
      }

      await _userRepository.UpdateUser(user, id);
      return NoContent();
   }

   [HttpGet]
   public async Task<IActionResult> GetAllUsers()
   {
      var users = await _userRepository.GetUsers();

      return Ok(users);
   }
   
   [HttpGet("{id}")]
   public async Task<ActionResult<User>> GetUserById(int id) {
      var user = await _userRepository.GetUserById(id);
      return Ok(user);
   }
   
   
}