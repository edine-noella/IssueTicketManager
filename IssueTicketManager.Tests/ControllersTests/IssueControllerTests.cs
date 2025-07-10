using FluentAssertions;
using IssueTicketManager.API.Controllers;
using IssueTicketManager.API.DTOs;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IssueTicketManager.Tests.ControllersTests;

[TestFixture]
public class IssueControllerTests
{
    
        private Mock<IIssueRepository> _mockIssueRepository;
        private Mock<IUserRepository> _mockUserRepository;
        private IssuesController _controller;

        [SetUp]
        public void Setup()
        {
            _mockIssueRepository = new Mock<IIssueRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _controller = new IssuesController(_mockIssueRepository.Object,  _mockUserRepository.Object);
        }

        [Test]
        public async Task Create_WithValidDto_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var dto = new CreateIssueDto
            {
                Title = "Test Issue",
                Body = "Test Body",
                CreatorId = 1,
                LabelIds = new List<int> { 1, 2 }
            };

            var expectedIssue = new Issue { Id = 1, Title = dto.Title };
            _mockIssueRepository.Setup(x => x.CreateIssueAsync(It.IsAny<Issue>()))
                .ReturnsAsync(expectedIssue);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var createdAtActionResult = result.Result as CreatedAtActionResult;
            createdAtActionResult.Should().NotBeNull();
            createdAtActionResult!.ActionName.Should().Be(nameof(_controller.GetIssue));
            createdAtActionResult.Value.Should().BeEquivalentTo(expectedIssue);
        }

        [Test]
        public async Task GetIssue_WithExistingId_ReturnsOkObjectResult()
        {
            // Arrange
            var issueId = 1;
            var expectedIssue = new Issue { Id = issueId };
            _mockIssueRepository.Setup(x => x.GetIssueByIdAsync(issueId))
                .ReturnsAsync(expectedIssue);

            // Act
            var result = await _controller.GetIssue(issueId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.Value.Should().BeEquivalentTo(expectedIssue);
        }

        [Test]
        public async Task GetIssue_WithNonExistingId_ReturnsNotFoundResult()
        {
            // Arrange
            var issueId = 999;
            _mockIssueRepository.Setup(x => x.GetIssueByIdAsync(issueId))
                .ReturnsAsync((Issue)null);

            // Act
            var result = await _controller.GetIssue(issueId);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Test]
        public async Task GetIssues_WhenCalled_ReturnsOkObjectResultWithIssues()
        {
            // Arrange
            var issues = new List<Issue>
            {
                new Issue { Id = 1 },
                new Issue { Id = 2 }
            };
            _mockIssueRepository.Setup(x => x.GetAllIssuesAsync())
                .ReturnsAsync(issues);

            // Act
            var result = await _controller.GetIssues();

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.Value.Should().BeEquivalentTo(issues);
          
        }

        [Test]
        public async Task Update_WithExistingIdAndValidDto_ReturnsOkObjectResult()
        {
            // Arrange
            var issueId = 1;
            var dto = new UpdateIssueDto { Title = "Updated Title" };
            var existingIssue = new Issue { Id = issueId };
            _mockIssueRepository.Setup(x => x.GetIssueByIdAsync(issueId))
                .ReturnsAsync(existingIssue);

            // Act
            var result = await _controller.Update(issueId, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockIssueRepository.Verify(x => x.UpdateIssueAsync(existingIssue), Times.Once);
        }

        [Test]
        public async Task Update_WithNonExistingId_ReturnsNotFoundResult()
        {
            // Arrange
            var issueId = 999;
            _mockIssueRepository.Setup(x => x.GetIssueByIdAsync(issueId))
                .ReturnsAsync((Issue)null);

            // Act
            var result = await _controller.Update(issueId, new UpdateIssueDto());

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Test]
        public async Task Update_WithNullLabelIds_DoesNotThrowException()
        {
            // Arrange
            var issueId = 1;
            var dto = new UpdateIssueDto { LabelIds = null };
            _mockIssueRepository.Setup(x => x.GetIssueByIdAsync(issueId))
                .ReturnsAsync(new Issue { Id = issueId });

            // Act
            Func<Task> act = async () => await _controller.Update(issueId, dto);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Test]
        public async Task Create_WithLabelIds_CreatesIssueWithLabels()
        {
            // Arrange
            var labelIds = new List<int> { 1, 2, 3 };
            var dto = new CreateIssueDto
            {
                Title = "Test",
                Body = "Test",
                CreatorId = 1,
                LabelIds = labelIds
            };

            Issue createdIssue = null;
            _mockIssueRepository.Setup(x => x.CreateIssueAsync(It.IsAny<Issue>()))
                .Callback<Issue>(i => createdIssue = i)
                .ReturnsAsync(new Issue { Id = 1 });

            // Act
            await _controller.Create(dto);

            // Assert
            createdIssue.Should().NotBeNull();
            createdIssue.IssueLabels.Should().HaveCount(labelIds.Count);
            createdIssue.IssueLabels.Select(il => il.LabelId).Should().BeEquivalentTo(labelIds);
        }
        
        // AssignIssue Tests -----
    [Test]
    public async Task AssignIssue_WithValidData_ReturnsOkObjectResult()
    {
        // Arrange
        var issueId = 1;
        var assigneeId = 10;
        var dto = new AssignUserIssueDto { AssigneeId = assigneeId };
        var existingIssue = new Issue { Id = issueId, Title = "Test Issue" };
        var updatedIssue = new Issue { Id = issueId, Title = "Test Issue", AssigneeId = assigneeId };

        _mockIssueRepository.Setup(x => x.GetIssueByIdAsync(issueId))
            .ReturnsAsync(existingIssue);
        _mockUserRepository.Setup(x => x.UserExists(assigneeId))
            .ReturnsAsync(true);
        _mockIssueRepository.Setup(x => x.UpdateIssueAsync(It.IsAny<Issue>()))
            .ReturnsAsync(updatedIssue);

        // Act
        var result = await _controller.AssignIssue(issueId, dto);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var returnedIssue = okResult!.Value as Issue;
        returnedIssue.Should().NotBeNull();
        returnedIssue!.AssigneeId.Should().Be(assigneeId);
        
        _mockIssueRepository.Verify(x => x.UpdateIssueAsync(It.Is<Issue>(i => i.AssigneeId == assigneeId)), Times.Once);
    }

    [Test]
    public async Task AssignIssue_WithNonExistingIssue_ReturnsNotFoundResult()
    {
        // Arrange
        var issueId = 999;
        var dto = new AssignUserIssueDto { AssigneeId = 10 };
        
        _mockIssueRepository.Setup(x => x.GetIssueByIdAsync(issueId))
            .ReturnsAsync((Issue)null);

        // Act
        var result = await _controller.AssignIssue(issueId, dto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        _mockUserRepository.Verify(x => x.UserExists(It.IsAny<int>()), Times.Never);
        _mockIssueRepository.Verify(x => x.UpdateIssueAsync(It.IsAny<Issue>()), Times.Never);
    }

   
    [Test]
    public async Task AssignIssue_WithNonExistingUser_ReturnsBadRequestResult()
    {
        // Arrange
        var issueId = 1;
        var assigneeId = 999;
        var dto = new AssignUserIssueDto { AssigneeId = assigneeId };
        var existingIssue = new Issue { Id = issueId };

        _mockIssueRepository.Setup(x => x.GetIssueByIdAsync(issueId))
            .ReturnsAsync(existingIssue);
        _mockUserRepository.Setup(x => x.UserExists(assigneeId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.AssignIssue(issueId, dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("Assignee not found");
        
        _mockIssueRepository.Verify(x => x.UpdateIssueAsync(It.IsAny<Issue>()), Times.Never);
    }
    
}