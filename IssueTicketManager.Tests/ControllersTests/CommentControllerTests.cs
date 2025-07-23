using FluentAssertions;
using IssueTicketManager.API.Controllers;
using IssueTicketManager.API.DTOs;
using IssueTicketManager.API.Messages;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using IssueTicketManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Microsoft.Extensions.Logging;

namespace IssueTicketManager.Tests.ControllersTests;

public class CommentControllerTests
{
    private Mock<ICommentRepository> _commentRepositoryMock;
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IIssueRepository> _issueRepositoryMock;
    private CommentController _commentController;
    private Mock<IServiceBusService > _serviceBusServiceMock;
    private Mock<ILogger<CommentController>> _loggerMock;
    

    [SetUp]
    public void SetUp()
    {
        _commentRepositoryMock = new Mock<ICommentRepository>();
        _issueRepositoryMock = new Mock<IIssueRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
         _serviceBusServiceMock = new Mock<IServiceBusService>();
         _loggerMock = new Mock<ILogger<CommentController>>();
        _commentController = new CommentController(
            _commentRepositoryMock.Object, 
            _issueRepositoryMock.Object,
            _userRepositoryMock.Object,
            _serviceBusServiceMock.Object,
            _loggerMock.Object
    );
    }
    
     [Test]
    public async Task AddComment_ShouldPublishCommentCreatedMessage_WhenSuccessful()
    {
        // Arrange
        var dto = new AddCommentDto
        {
            Text = "Test comment",
            UserId = 1,
            IssueId = 101
        };

        var createdComment = new Comment
        {
            Id = 1,
            Text = dto.Text,
            UserId = dto.UserId,
            IssueId = dto.IssueId
        };

        _issueRepositoryMock.Setup(repo => repo.IssueExistsAsync(dto.IssueId))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(repo => repo.UserExists(dto.UserId))
            .ReturnsAsync(true);
        _commentRepositoryMock.Setup(r => r.AddCommentAsync(It.IsAny<Comment>()))
            .ReturnsAsync(createdComment);
        _commentRepositoryMock.Setup(r => r.GetCommentWithDetailsAsync(createdComment.Id))
            .ReturnsAsync(createdComment);

        IssueCommentCreatedMessage capturedMessage = null;
        _serviceBusServiceMock.Setup(s => s.PublishIssueCommentCreatedAsync(It.IsAny<IssueCommentCreatedMessage>(), It.IsAny<CancellationToken>()))
            .Callback<IssueCommentCreatedMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _commentController.AddComment(dto);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        
        // Verify Service Bus interaction
        _serviceBusServiceMock.Verify(
            x => x.PublishIssueCommentCreatedAsync(It.IsAny<IssueCommentCreatedMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);

        capturedMessage.Should().NotBeNull();
        capturedMessage.CommentId.Should().Be(createdComment.Id);
        capturedMessage.Content.Should().Be(dto.Text);
        capturedMessage.UserId.Should().Be(dto.UserId);
        capturedMessage.IssueId.Should().Be(dto.IssueId);
        capturedMessage.EventType.Should().Be("issue.comment.create");
    }

    [Test]
    public async Task AddComment_ShouldNotPublishMessage_WhenModelStateIsInvalid()
    {
        // Arrange
        var dto = new AddCommentDto
        {
            Text = "", // Invalid
            UserId = -1, // Invalid
            IssueId = 0 // Invalid
        };

        _commentController.ModelState.AddModelError("Text", "Required");
        _commentController.ModelState.AddModelError("UserId", "Invalid");
        _commentController.ModelState.AddModelError("IssueId", "Invalid");

        // Act
        var result = await _commentController.AddComment(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _serviceBusServiceMock.Verify(
            x => x.PublishIssueCommentCreatedAsync(It.IsAny<IssueCommentCreatedMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    
    [Test]
    public async Task? GetComment_ShouldReturnNotFound_WhenCommentDoesNttExist()
    {
        // Arrange
        const int nonExistingId = 999;
        _commentRepositoryMock
            .Setup(r => r.GetCommentWithDetailsAsync(nonExistingId))
            .ReturnsAsync((Comment?)null);
        
        // Act
        var result = await _commentController.GetComment(nonExistingId);
        
        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
    
    [Test]
    public async Task GetComment_ShouldReturnOkResult_WhenCommentExists()
    {
        // Arrange
        const int existingId = 1;
        
        var existingComment = new Comment
        {
            Id = existingId,
            Text = "Test comment 1",
            UserId = 1,
            IssueId = 101
        };

        _commentRepositoryMock
            .Setup(r => r.GetCommentWithDetailsAsync(existingId))
            .ReturnsAsync(existingComment);
        
        // Act
        var result = await _commentController.GetComment(existingId);
        
        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(existingComment));
    }

    [Test]
    public async Task GetAllComments_ShouldReturnWithEmptyList_WhenNoCommentsExist()
    {
        // Arrange
        _commentRepositoryMock.Setup(r => r.GetAllCommentsAsync()).ReturnsAsync(new List<Comment>());
        
        // Act
        var result = await _commentController.GetAllComments();
        
        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.EqualTo(new List<Comment>()));
    }
    
    [Test]
    public async Task GetAllComments_ShouldReturnOkWithComments_WhenCommentsExist()
    {
        // Arrange
        var expectedComments = new List<Comment>
        {
            new Comment { Id = 1, Text = "Comment 1", UserId = 1, IssueId = 1 },
            new Comment { Id = 2, Text = "Comment 2", UserId = 2, IssueId = 1 }
        };

        _commentRepositoryMock.Setup(r => r.GetAllCommentsAsync())
            .ReturnsAsync(expectedComments);

        // Act
        var result = await _commentController.GetAllComments();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo(expectedComments));
    }
    
    [Test]
    public async Task AddComment_ShouldHandleRepositoryException_WhenAddFails()
    {
        // Arrange
        var dto = new AddCommentDto
        {
            Text = "Test comment",
            UserId = 1,
            IssueId = 1
        };

        _issueRepositoryMock.Setup(repo => repo.IssueExistsAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        _userRepositoryMock.Setup(repo => repo.UserExists(It.IsAny<int>()))
            .ReturnsAsync(true);

        _commentRepositoryMock
            .Setup(r => r.AddCommentAsync(It.IsAny<Comment>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        Assert.ThrowsAsync<Exception>(() => _commentController.AddComment(dto));
    }

    [Test]
    public async Task GetComment_ShouldHandleRepositoryException_WhenGetFails()
    {
        // Arrange
        int commentId = 1;
        _commentRepositoryMock.Setup(r => r.GetCommentWithDetailsAsync(commentId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        Assert.ThrowsAsync<Exception>(() => _commentController.GetComment(commentId));
    }

    [Test]
    public async Task GetAllComments_ShouldHandleRepositoryException_WhenGetAllFails()
    {
        // Arrange
        _commentRepositoryMock.Setup(r => r.GetAllCommentsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        Assert.ThrowsAsync<Exception>(() => _commentController.GetAllComments());
    }
    
}