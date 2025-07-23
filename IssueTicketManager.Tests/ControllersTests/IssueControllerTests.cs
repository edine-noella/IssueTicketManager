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

namespace IssueTicketManager.Tests.ControllersTests
{
    [TestFixture]
    public class IssueControllerIntegrationTests
    {
        private Mock<IIssueRepository> _mockIssueRepository;
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<IServiceBusService> _mockServiceBusService;
        private Mock<ILogger<IssuesController>> _mockLogger;
        private IssuesController _controller;

        [SetUp]
        public void Setup()
        {
            _mockIssueRepository = new Mock<IIssueRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockServiceBusService = new Mock<IServiceBusService>();
            _mockLogger = new Mock<ILogger<IssuesController>>();
            
            _controller = new IssuesController(
                _mockIssueRepository.Object,
                _mockUserRepository.Object,
                _mockServiceBusService.Object,
                _mockLogger.Object);
        }

        [Test]
        public async Task Create_WithValidDto_PublishesIssueCreatedMessage()
        {
            // Arrange
            var dto = new CreateIssueDto
            {
                Title = "Test Issue",
                Body = "Test Body",
                CreatorId = 1,
                LabelIds = new List<int> { 1, 2 }
            };

            var expectedIssue = new Issue 
            { 
                Id = 1, 
                Title = dto.Title, 
                Body = dto.Body, 
                CreatorId = dto.CreatorId 
            };

            _mockIssueRepository.Setup(x => x.CreateIssueAsync(It.IsAny<Issue>()))
                .ReturnsAsync(expectedIssue);

            IssueCreatedMessage capturedMessage = null;
            _mockServiceBusService.Setup(x => x.PublishIssueCreatedAsync(It.IsAny<IssueCreatedMessage>(), It.IsAny<CancellationToken>()))
                .Callback<IssueCreatedMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var createdAtActionResult = result.Result as CreatedAtActionResult;
            createdAtActionResult.Should().NotBeNull();
            createdAtActionResult!.Value.Should().BeEquivalentTo(expectedIssue);

            _mockServiceBusService.Verify(x => x.PublishIssueCreatedAsync(It.IsAny<IssueCreatedMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            
            capturedMessage.Should().NotBeNull();
            capturedMessage.IssueId.Should().Be(expectedIssue.Id);
            capturedMessage.Title.Should().Be(dto.Title);
            capturedMessage.Body.Should().Be(dto.Body);
            capturedMessage.CreatorId.Should().Be(dto.CreatorId);
            capturedMessage.LabelIds.Should().BeEquivalentTo(dto.LabelIds);
            capturedMessage.EventType.Should().Be("issue.create");
        }

        [Test]
        public async Task Create_WhenServiceBusThrowsException_ReturnsInternalServerError()
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

            _mockServiceBusService.Setup(x => x.PublishIssueCreatedAsync(It.IsAny<IssueCreatedMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service Bus error"));

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var statusCodeResult = result.Result as ObjectResult;
            statusCodeResult.Should().NotBeNull();
            statusCodeResult!.StatusCode.Should().Be(500);
            statusCodeResult.Value.Should().Be("An error occurred while creating the issue");
        }

        [Test]
        public async Task Update_WithValidDto_PublishesIssueUpdatedMessage()
        {
            // Arrange
            var issueId = 1;
            var dto = new UpdateIssueDto 
            { 
                Title = "Updated Title", 
                Body = "Updated Body",
                Status = IssueStatus.InProgress
            };

            var existingIssue = new Issue { Id = issueId, Title = "Original Title" };
            var updatedIssue = new Issue { Id = issueId, Title = dto.Title, Body = dto.Body };

            _mockIssueRepository.Setup(x => x.GetIssueByIdAsync(issueId))
                .ReturnsAsync(existingIssue);
            _mockIssueRepository.Setup(x => x.UpdateIssueAsync(It.IsAny<Issue>()))
                .ReturnsAsync(updatedIssue);

            IssueUpdatedMessage capturedMessage = null;
            _mockServiceBusService.Setup(x => x.PublishIssueUpdatedAsync(It.IsAny<IssueUpdatedMessage>(), It.IsAny<CancellationToken>()))
                .Callback<IssueUpdatedMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(issueId, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockServiceBusService.Verify(x => x.PublishIssueUpdatedAsync(It.IsAny<IssueUpdatedMessage>(), It.IsAny<CancellationToken>()), Times.Once);

            capturedMessage.Should().NotBeNull();
            capturedMessage.IssueId.Should().Be(issueId);
            capturedMessage.Title.Should().Be(dto.Title);
            capturedMessage.Body.Should().Be(dto.Body);
            capturedMessage.Status.Should().Be(dto.Status.ToString());
            capturedMessage.EventType.Should().Be("issue.update");
        }

        [Test]
        public async Task AssignIssue_WithValidData_PublishesIssueAssignedMessage()
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

            IssueAssignedMessage capturedMessage = null;
            _mockServiceBusService.Setup(x => x.PublishIssueAssignedAsync(It.IsAny<IssueAssignedMessage>(), It.IsAny<CancellationToken>()))
                .Callback<IssueAssignedMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AssignIssue(issueId, dto);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            
            _mockServiceBusService.Verify(x => x.PublishIssueAssignedAsync(It.IsAny<IssueAssignedMessage>(), It.IsAny<CancellationToken>()), Times.Once);

            capturedMessage.Should().NotBeNull();
            capturedMessage.IssueId.Should().Be(issueId);
            capturedMessage.EventType.Should().Be("issue.user.assign");
        }

        [Test]
        public async Task AssignIssue_WithNonExistingIssue_DoesNotPublishMessage()
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
            _mockServiceBusService.Verify(x => x.PublishIssueAssignedAsync(It.IsAny<IssueAssignedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task AddLabelToIssue_WithValidData_PublishesIssueLabelAssignedMessage()
        {
            // Arrange
            var issueId = 1;
            var labelId = 2;
            var dto = new AddLabelToIssueDto { LabelId = labelId };
            
            var updatedIssue = new Issue { Id = issueId, Title = "Test Issue" };

            _mockIssueRepository.Setup(x => x.AddLabelToIssueAsync(issueId, labelId))
                .ReturnsAsync((updatedIssue, LabelAddResult.Success));

            IssueLabelAssignedMessage capturedMessage = null;
            _mockServiceBusService.Setup(x => x.PublishIssueLabelAssignedAsync(It.IsAny<IssueLabelAssignedMessage>(), It.IsAny<CancellationToken>()))
                .Callback<IssueLabelAssignedMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AddLabelToIssue(issueId, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockServiceBusService.Verify(x => x.PublishIssueLabelAssignedAsync(It.IsAny<IssueLabelAssignedMessage>(), It.IsAny<CancellationToken>()), Times.Once);

            capturedMessage.Should().NotBeNull();
            capturedMessage.IssueId.Should().Be(issueId);
            capturedMessage.LabelId.Should().Be(labelId);
            capturedMessage.EventType.Should().Be("issue.label.assign");
        }

        [Test]
        public async Task AddLabelToIssue_WhenIssueNotFound_DoesNotPublishMessage()
        {
            // Arrange
            var issueId = 999;
            var labelId = 2;
            var dto = new AddLabelToIssueDto { LabelId = labelId };

            _mockIssueRepository.Setup(x => x.AddLabelToIssueAsync(issueId, labelId))
                .ReturnsAsync((null, LabelAddResult.IssueNotFound));

            // Act
            var result = await _controller.AddLabelToIssue(issueId, dto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            _mockServiceBusService.Verify(x => x.PublishIssueLabelAssignedAsync(It.IsAny<IssueLabelAssignedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task AddLabelToIssue_WhenLabelAlreadyAssigned_DoesNotPublishMessage()
        {
            // Arrange
            var issueId = 1;
            var labelId = 2;
            var dto = new AddLabelToIssueDto { LabelId = labelId };
            
            var existingIssue = new Issue { Id = issueId, Title = "Test Issue" };

            _mockIssueRepository.Setup(x => x.AddLabelToIssueAsync(issueId, labelId))
                .ReturnsAsync((existingIssue, LabelAddResult.AlreadyAssigned));

            // Act
            var result = await _controller.AddLabelToIssue(issueId, dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _mockServiceBusService.Verify(x => x.PublishIssueLabelAssignedAsync(It.IsAny<IssueLabelAssignedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}