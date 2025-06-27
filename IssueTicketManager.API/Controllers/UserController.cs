using IssueTicketManager.API.DTOs;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories;
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
   public async Task<ActionResult<User>> CreateUser([FromBody] User user)
   {
      if (!ModelState.IsValid)
      {
         return BadRequest(ModelState);
      }

      try
      {
         var existingUser = await _userRepository.GetUserByEmail(user.Email);
         if(existingUser != null) throw new Microsoft.EntityFrameworkCore.DbUpdateException();
         
         await _userRepository.CreateUser(user);
      }
      catch (Microsoft.EntityFrameworkCore.DbUpdateException e)
      {
         return Conflict(e.Message);
      }
      
      return CreatedAtAction(nameof(GetUserByEmail), new { id = user.Id }, user);
   }

   [HttpPut("{email}")]
   public async Task<IActionResult> UpdateUser( string email, UpdateUserDto user)
   {
      if (!ModelState.IsValid)
      {
         return BadRequest(ModelState);
      }

      try
      {
         var existingUser = await _userRepository.GetUserByEmail(email);
         if(existingUser == null) throw new KeyNotFoundException();
         existingUser.Name = user.Name;
         existingUser.Email = user.Email;
         
         await _userRepository.UpdateUser(existingUser);
         return NoContent();

      }
      catch (KeyNotFoundException)
      {
         return NotFound(new { message = "User not found." });
      }
      catch(Microsoft.EntityFrameworkCore.DbUpdateException)
      {
         return Conflict(new { message = "User with email already exists." });
      }
     
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