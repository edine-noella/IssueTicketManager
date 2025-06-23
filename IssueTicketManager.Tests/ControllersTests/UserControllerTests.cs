using FluentAssertions;
using IssueTicketManager.API.Controllers;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories;
using Microsoft.AspNetCore.Mvc;
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
        // result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Test]
    public async Task UpdateUser_ShouldReturnNoContent()
    {
        //Arrange
        var user = new User { Name = "Becca", Email = "becca@gmail.com" };
        const int id = 1;
        _mockRepository
            .Setup(r => r.GetUserById(id))
            .ReturnsAsync(user);
        //Act
        var result = await _controller.UpdateUser(id, user);
        
        //Assert
        result.Should().BeOfType<NoContentResult>();
    }
    
    [Test]
    public async Task UpdateUser_ShouldReturnNotFound()
    {
        //Arrange
        const int id = 1;
        _mockRepository
            .Setup(r => r.GetUserById(id))
            .ThrowsAsync(new KeyNotFoundException("User not found."));
        //Act
        var result = await _controller.UpdateUser(id, new User());
        
        //Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UpdateUser_ShouldReturnConflict_ForDuplicateEmail()
    {
        var user = new User { Name = "Becca", Email = "becca@gmail.com" };
        var duplicateUser = new User { Id = 2, Name = "Joyce", Email = "becca@gmail.com" };

        const int id = 1;
        _mockRepository
            .Setup(u => u.GetUserById(id))
            .ReturnsAsync(user);

        _mockRepository
            .Setup(u => u.GetUserByEmail(user.Email))
            .ReturnsAsync(duplicateUser);
        
        // Act
        var result = await _controller.UpdateUser(id, user);
        
        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }
   
}