using IssueTicketManager.API.Controllers;
using IssueTicketManager.API.DTOs;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IssueTicketManager.Tests.ControllersTests;

public class CommentControllerTests
{
    private Mock<ICommentRepository> _commentRepositoryMock;
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IIssueRepository> _issueRepositoryMock;
    private CommentController _commentController;

    [SetUp]
    public void SetUp()
    {
        _commentRepositoryMock = new Mock<ICommentRepository>();
        _issueRepositoryMock = new Mock<IIssueRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _commentController = new CommentController(
            _commentRepositoryMock.Object, 
            _issueRepositoryMock.Object,
            _userRepositoryMock.Object
            );
    }
    
    [Test]
    public async Task AddComment_ShouldReturnBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        var dto = new AddCommentDto
        {
            Text = "", 
            UserId = -1, 
            IssueId = 0  
        };

        _commentController.ModelState.AddModelError("Text", "Text is required.");
        _commentController.ModelState.AddModelError("UserId", "UserId must be greater than 0.");
        _commentController.ModelState.AddModelError("IssueId", "IssueId must be greater than 0.");

        // Act
        var result = await _commentController.AddComment(dto);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = result as BadRequestObjectResult;
        Assert.That(badRequest!.Value, Is.TypeOf<SerializableError>());
    }

    [Test]
    public async Task AddComment_ShouldReturnCreatedActionResult_WhenValidRequest()
    {
        // Arrange
        var dto = new AddCommentDto
        {
            Text = "Test comment 1",
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

        _issueRepositoryMock.Setup(repo => repo.IssueExistsAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        _userRepositoryMock.Setup(repo => repo.UserExists(It.IsAny<int>()))
            .ReturnsAsync(true);

        _commentRepositoryMock
            .Setup(r => r.AddCommentAsync(It.IsAny<Comment>()))
            .ReturnsAsync(createdComment);
        
        _commentRepositoryMock
            .Setup(r => r.GetCommentWithDetailsAsync(It.IsAny<int>()))
            .ReturnsAsync(createdComment);
        
        
        // Act
        _commentController.ModelState.Clear();
        if (string.IsNullOrWhiteSpace(dto.Text))
        {
            _commentController.ModelState.AddModelError("Text", "Text is required");
        }
        var result = await _commentController.AddComment(dto);
        
        // Assert
        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = (CreatedAtActionResult)result;
        Assert.That(createdResult.ActionName, Is.EqualTo("GetComment"));
        Assert.That(createdResult.Value, Is.EqualTo(createdComment));
    }
}