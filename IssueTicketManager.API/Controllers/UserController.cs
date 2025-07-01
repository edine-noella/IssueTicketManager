using IssueTicketManager.API.DTOs;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IssueTicketManager.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
   private readonly IUserRepository _userRepository;

   public UserController(IUserRepository userRepository)
   {
      _userRepository = userRepository;
   }

   [HttpPost]
   public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserDto user)
   {
      if (!ModelState.IsValid)
      {
         return BadRequest(ModelState);
      }
      
      var existingUser = await _userRepository.GetUserByEmail(user.Email);
      if(existingUser != null)  return Conflict((new { message = "User with email already exists." }));

      var newUser = new User
         {
            Name = user.Name,
            Email = user.Email
         };
         
         await _userRepository.CreateUser(newUser);
         return CreatedAtAction(nameof(GetUserByEmail), new { email = newUser.Email }, newUser);

      
     
   }

   [HttpPut("{email}")]
   public async Task<IActionResult> UpdateUser( string email, UpdateUserDto user)
   {
      if (!ModelState.IsValid)
      {
         return BadRequest(ModelState);
      }
      
      var existingUser = await _userRepository.GetUserByEmail(email);
      if(existingUser == null) return NotFound("User not found");

      // Check if user with new email already exists
      var userWithNewEmail = await _userRepository.GetUserByEmail(user.Email);
      if (userWithNewEmail != null && userWithNewEmail.Id != existingUser.Id)
      {
            return Conflict((new { message = "User with email already exists." }));
      }
      existingUser.Name = user.Name;
      existingUser.Email = user.Email;
         
         await _userRepository.UpdateUser(existingUser);
         return Ok(new
         {
            message = "User updated successfully",
            user = existingUser
         });
      
   }

   [HttpGet]
   public async Task<IActionResult> GetAllUsers()
   {
      var users = await _userRepository.GetUsers();

      return Ok(users);
   }
   
   [HttpGet("{email}")]
   public async Task<ActionResult<User>> GetUserByEmail(string email) {
      var user = await _userRepository.GetUserByEmail(email);
      return Ok(user);
   }
   
   
}