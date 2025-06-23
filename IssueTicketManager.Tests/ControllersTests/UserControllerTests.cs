using FluentAssertions;
using IssueTicketManager.API.Controllers;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
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
   
}