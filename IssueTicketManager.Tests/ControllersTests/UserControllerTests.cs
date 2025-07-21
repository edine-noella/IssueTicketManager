using FluentAssertions;
using IssueTicketManager.API.Controllers;
using IssueTicketManager.API.DTOs;
using IssueTicketManager.API.Messages;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using IssueTicketManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace IssueTicketManager.Tests.ControllersTests;

[TestFixture]
public class UserControllerTests
{
    private Mock<IUserRepository> _mockRepository;
    private Mock<IServiceBusService> _mockServiceBusService;
    private Mock<ILogger<UserController>> _mockLogger;
    private UserController _controller;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockServiceBusService = new Mock<IServiceBusService>();
        _mockLogger = new Mock<ILogger<UserController>>();
        
        _controller = new UserController(
            _mockRepository.Object, 
            _mockServiceBusService.Object, 
            _mockLogger.Object);
    }

    [Test]
    public async Task CreateUser_ShouldPublishUserCreatedMessage_WhenSuccessful()
    {
        // Arrange
        var userDto = new CreateUserDto { Name = "Becca", Email = "becca@gmail.com" };
        var expectedUser = new User { Id = 1, Name = userDto.Name, Email = userDto.Email };

        _mockRepository.Setup(r => r.GetUserByEmail(userDto.Email))
            .ReturnsAsync((User?)null);
        _mockRepository.Setup(r => r.CreateUser(It.IsAny<User>()))
            .Callback<User>(u => u.Id = expectedUser.Id)
            .Returns(Task.CompletedTask);

        UserCreatedMessage capturedMessage = null;
        _mockServiceBusService.Setup(s => s.PublishUserCreatedAsync(It.IsAny<UserCreatedMessage>(), It.IsAny<CancellationToken>()))
            .Callback<UserCreatedMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CreateUser(userDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        
        // Verify Service Bus interaction
        _mockServiceBusService.Verify(
            x => x.PublishUserCreatedAsync(It.IsAny<UserCreatedMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);

        capturedMessage.Should().NotBeNull();
        capturedMessage.UserId.Should().Be(expectedUser.Id);
        capturedMessage.Username.Should().Be(userDto.Name);
        capturedMessage.Email.Should().Be(userDto.Email);
        capturedMessage.EventType.Should().Be("user.create");
    }

    [Test]
    public async Task CreateUser_ShouldNotPublishMessage_WhenEmailExists()
    {
        // Arrange
        var userDto = new CreateUserDto { Name = "Becca", Email = "exists@gmail.com" };
        var existingUser = new User { Id = 1, Name = "Existing", Email = userDto.Email };

        _mockRepository.Setup(r => r.GetUserByEmail(userDto.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _controller.CreateUser(userDto);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
        _mockServiceBusService.Verify(
            x => x.PublishUserCreatedAsync(It.IsAny<UserCreatedMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CreateUser_ShouldLogError_WhenServiceBusFails()
    {
        // Arrange
        var userDto = new CreateUserDto { Name = "Becca", Email = "becca@gmail.com" };

        _mockRepository.Setup(r => r.GetUserByEmail(userDto.Email))
            .ReturnsAsync((User?)null);
        _mockRepository.Setup(r => r.CreateUser(It.IsAny<User>()))
            .Callback<User>(u => u.Id = 1)
            .Returns(Task.CompletedTask);

        _mockServiceBusService.Setup(s => s.PublishUserCreatedAsync(It.IsAny<UserCreatedMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service Bus error"));

        // Act
        var result = await _controller.CreateUser(userDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>(); // User creation should succeed
        
        // Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to publish user created message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
    
    [Test]
    public async Task UpdateUser_ShouldReturnBadRequest_IfModelStateIsInvalid()
    {
        //Arrange
        _controller.ModelState.AddModelError("Email", "Required");
        
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