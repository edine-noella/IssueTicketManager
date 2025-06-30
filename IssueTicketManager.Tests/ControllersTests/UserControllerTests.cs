using FluentAssertions;
using IssueTicketManager.API.Controllers;
using IssueTicketManager.API.DTOs;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories;
using IssueTicketManager.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace IssueTicketManager.Tests.ControllersTests;

[TestFixture]
public class UserControllerTests
{
    private Mock<IUserRepository> _mockRepository;
    private UserController _controller;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IUserRepository>();
        _controller = new UserController(_mockRepository.Object);
    }

    [Test]
    public async Task CreateUser_ReturnsSuccess_andUserObject()
    {
        // Arrange
        var user = new User { Name = "Becca", Email = "becca@gmail.com" };
        
        // Act
        var result = await _controller.CreateUser(user);
        
        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }
    
    [Test]
    public async Task CreateUser_ThrowsBadRequest_ifModelStateIsInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("Email", "Email is required");
        var user = new User { Name = "Becca" };
        
        // Act
        var result = await _controller.CreateUser(user);
        
        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
    
    [Test]
    public async Task CreateUser_ReturnsConflictsMessageForDuplicateEmail()
    {
        // Arrange
        var user = new User { Name = "Becca", Email = "becca@gmail.com" };
        var duplicateUser = new User { Name = "Joyce", Email = "becca@gmail.com" };

        _mockRepository
            .SetupSequence(r => r.GetUserByEmail("becca@gmail.com"))
            .ReturnsAsync((User?)null)
            .ReturnsAsync(user);
        // Act
        var result = await _controller.CreateUser(user);
        var result2 = await _controller.CreateUser(duplicateUser);
        
        // Assert
        result2.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Test]
    public async Task UpdateUser_ShouldReturnBadRequst_IfModelStateIsInvalid()
    {
        //Arrange
        _controller.ModelState.AddModelError("Emaill", "Required");
        
        // Act
        var result = await _controller.UpdateUser("rbccm@gmail.com", new UpdateUserDto());
        
        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
    
    [Test]
    public async Task UpdateUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        //Arrange
        const string email  = "beccatoni@gmail.com";
        _mockRepository
            .Setup(r => r.GetUserByEmail(email))
            .ReturnsAsync((User?)null);
        //Act
        var result = await _controller.UpdateUser( email, new UpdateUserDto());
        
        //Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UpdateUser_ShouldReturnConflict_ForDuplicateEmail()
    {
        const string currentEmail = "becca@gmail.com";
        var updateDto = new UpdateUserDto
        {
            Name = "Joyce",
            Email = "joyce@gmail.com" // Trying to update to a duplicate email
        };

        // The user to be updated
        var existingUser = new User
        {
            Id = 1,
            Name = "Becca",
            Email = currentEmail
        };
        
        // The user with the duplicate email
        var duplicateUser = new User
        {
            Id = 2,
            Name = "Joyce",
            Email = "joyce@gmail.com"
        };

        _mockRepository
            .Setup(r => r.GetUserByEmail(currentEmail))
            .ReturnsAsync(existingUser);
        _mockRepository
            .Setup(r => r.GetUserByEmail(updateDto.Email))
            .ReturnsAsync(duplicateUser);
        
        
        // Act
        var result = await _controller.UpdateUser(currentEmail, updateDto);
        
        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = result as ConflictObjectResult;
        conflictResult?.Value.Should().BeEquivalentTo(new { message = "User with email already exists." });
    }

    [Test]
    public async Task UpdateUser_ShouldReturnOk_WhenUpdateSucceeds()
    {
        // Arrange
        var email = "mutoni@gmail.com";
        var updateUserDto = new UpdateUserDto() { Name = "Mutoni", Email = "mumu@gmail.com" };

        var existingUser = new User { Id = 4, Name = "Umutoni", Email = "umutoni@gmail.com" };

        _mockRepository
            .Setup(r => r.GetUserByEmail(email))
            .ReturnsAsync(existingUser);
        
        _mockRepository
            .Setup(r => r.GetUserByEmail(updateUserDto.Email))
            .ReturnsAsync((User?)null);

        _mockRepository
            .Setup(r => r.UpdateUser(It.IsAny<User>()))
            .Returns(Task.CompletedTask);
        // Act
        var result = await _controller.UpdateUser(email, updateUserDto);
        
        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value!
            .GetType().GetProperty("message")!
            .GetValue(okResult.Value, null);
        response.Should().Be("User updated successfully");
        result.Should().BeOfType<OkObjectResult>();
    }

}