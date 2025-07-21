using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using IssueTicketManager.API.Configuration;
using IssueTicketManager.API.Messages;
using IssueTicketManager.API.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace IssueTicketManager.Tests.ServicesTests
{
    [TestFixture]
    public class ServiceBusServiceTests
    {
        private Mock<ServiceBusClient> _mockClient;
        private Mock<ServiceBusSender> _mockSender;
        private Mock<ILogger<ServiceBusService>> _mockLogger;
        private Mock<IOptions<ServiceBusConfiguration>> _mockOptions;
        private ServiceBusConfiguration _configuration;
        private ServiceBusService _serviceBusService;

        [SetUp]
        public void Setup()
        {
            _mockClient = new Mock<ServiceBusClient>();
            _mockSender = new Mock<ServiceBusSender>();
            _mockLogger = new Mock<ILogger<ServiceBusService>>();
            _mockOptions = new Mock<IOptions<ServiceBusConfiguration>>();

            _configuration = new ServiceBusConfiguration
            {
                ConnectionString = "test-connection-string",
                Topics = new TopicConfiguration
                {
                    UserCreate = "user.create",
                    LabelCreate = "label.create",
                    IssueCreate = "issue.create",
                    IssueUpdate = "issue.update",
                    IssueUserAssign = "issue.user.assign",
                    IssueCommentCreate = "issue.comment.create",
                    IssueLabelAssign = "issue.label.assign"
                }
            };

            _mockOptions.Setup(x => x.Value).Returns(_configuration);
            _mockClient.Setup(x => x.CreateSender(It.IsAny<string>())).Returns(_mockSender.Object);

            _serviceBusService = new ServiceBusService(_mockClient.Object, _mockOptions.Object, _mockLogger.Object);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _serviceBusService.DisposeAsync();
        }

        [Test]
        public async Task PublishIssueCreatedAsync_WithValidMessage_CallsSendMessageAsync()
        {
            // Arrange
            var message = new IssueCreatedMessage
            {
                IssueId = 1,
                Title = "Test Issue",
                Body = "Test Body",
                CreatorId = 1,
                LabelIds = new List<int> { 1, 2 }
            };

            ServiceBusMessage capturedMessage = null;
            _mockSender.Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            await _serviceBusService.PublishIssueCreatedAsync(message);

            // Assert
            _mockSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockClient.Verify(x => x.CreateSender(_configuration.Topics.IssueCreate), Times.Once);

            capturedMessage.Should().NotBeNull();
            capturedMessage.ContentType.Should().Be("application/json");
            capturedMessage.CorrelationId.Should().Be(message.CorrelationId);
            capturedMessage.Subject.Should().Be("issue.create");
            capturedMessage.ApplicationProperties["EventType"].Should().Be("issue.create");

            var deserializedMessage = JsonSerializer.Deserialize<IssueCreatedMessage>(capturedMessage.Body.ToString());
            deserializedMessage.Should().BeEquivalentTo(message);
        }

        [Test]
        public async Task PublishIssueAssignedAsync_WithValidMessage_CallsSendMessageAsync()
        {
            // Arrange
            var message = new IssueAssignedMessage
            {
                IssueId = 1,
                AssigneeId = 2,
                AssignedByUserId = 3
            };

            ServiceBusMessage capturedMessage = null;
            _mockSender.Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            await _serviceBusService.PublishIssueAssignedAsync(message);

            // Assert
            _mockSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockClient.Verify(x => x.CreateSender(_configuration.Topics.IssueUserAssign), Times.Once);

            capturedMessage.Should().NotBeNull();
            capturedMessage.Subject.Should().Be("issue.user.assign");
            capturedMessage.ApplicationProperties["EventType"].Should().Be("issue.user.assign");

            var deserializedMessage = JsonSerializer.Deserialize<IssueAssignedMessage>(capturedMessage.Body.ToString());
            deserializedMessage.Should().BeEquivalentTo(message);
        }

        [Test]
        public async Task PublishIssueLabelAssignedAsync_WithValidMessage_CallsSendMessageAsync()
        {
            // Arrange
            var message = new IssueLabelAssignedMessage
            {
                IssueId = 1,
                LabelId = 2,
                AssignedByUserId = 3
            };

            ServiceBusMessage capturedMessage = null;
            _mockSender.Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Callback<ServiceBusMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
                .Returns(Task.CompletedTask);

            // Act
            await _serviceBusService.PublishIssueLabelAssignedAsync(message);

            // Assert
            _mockSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockClient.Verify(x => x.CreateSender(_configuration.Topics.IssueLabelAssign), Times.Once);

            capturedMessage.Should().NotBeNull();
            capturedMessage.Subject.Should().Be("issue.label.assign");
            capturedMessage.ApplicationProperties["EventType"].Should().Be("issue.label.assign");

            var deserializedMessage = JsonSerializer.Deserialize<IssueLabelAssignedMessage>(capturedMessage.Body.ToString());
            deserializedMessage.Should().BeEquivalentTo(message);
        }

        [Test]
        public async Task PublishAsync_WhenSenderThrowsException_LogsErrorAndRethrows()
        {
            // Arrange
            var message = new IssueCreatedMessage
            {
                IssueId = 1,
                Title = "Test Issue"
            };

            var expectedException = new ServiceBusException("Test exception", ServiceBusFailureReason.GeneralError);
            _mockSender.Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ServiceBusException>(() => 
                _serviceBusService.PublishIssueCreatedAsync(message));

            exception.Should().Be(expectedException);
            
            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to publish message")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task PublishUserCreatedAsync_WithValidMessage_PublishesToCorrectTopic()
        {
            // Arrange
            var message = new UserCreatedMessage
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@example.com"
            };

            // Act
            await _serviceBusService.PublishUserCreatedAsync(message);

            // Assert
            _mockClient.Verify(x => x.CreateSender(_configuration.Topics.UserCreate), Times.Once);
            _mockSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PublishLabelCreatedAsync_WithValidMessage_PublishesToCorrectTopic()
        {
            // Arrange
            var message = new LabelCreatedMessage
            {
                LabelId = 1,
                Name = "Bug",
                Color = "red"
            };

            // Act
            await _serviceBusService.PublishLabelCreatedAsync(message);

            // Assert
            _mockClient.Verify(x => x.CreateSender(_configuration.Topics.LabelCreate), Times.Once);
            _mockSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PublishIssueUpdatedAsync_WithValidMessage_PublishesToCorrectTopic()
        {
            // Arrange
            var message = new IssueUpdatedMessage
            {
                IssueId = 1,
                Title = "Updated Title",
                Status = "InProgress"
            };

            // Act
            await _serviceBusService.PublishIssueUpdatedAsync(message);

            // Assert
            _mockClient.Verify(x => x.CreateSender(_configuration.Topics.IssueUpdate), Times.Once);
            _mockSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PublishIssueCommentCreatedAsync_WithValidMessage_PublishesToCorrectTopic()
        {
            // Arrange
            var message = new IssueCommentCreatedMessage
            {
                CommentId = 1,
                IssueId = 1,
                UserId = 1,
                Content = "Test comment"
            };

            // Act
            await _serviceBusService.PublishIssueCommentCreatedAsync(message);

            // Assert
            _mockClient.Verify(x => x.CreateSender(_configuration.Topics.IssueCommentCreate), Times.Once);
            _mockSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetOrCreateSenderAsync_CalledMultipleTimes_ReusesExistingSender()
        {
            // Arrange
            var topicName = "test.topic";
            var message = new IssueCreatedMessage { IssueId = 1 };

            // Act
            await _serviceBusService.PublishAsync(message, topicName);
            await _serviceBusService.PublishAsync(message, topicName);

            // Assert
            _mockClient.Verify(x => x.CreateSender(topicName), Times.Once);
            _mockSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task DisposeAsync_WhenCalled_DisposesAllSendersAndClient()
        {
            // Arrange
            var message = new IssueCreatedMessage { IssueId = 1 };
            await _serviceBusService.PublishIssueCreatedAsync(message);

            // Act
            await _serviceBusService.DisposeAsync();

            // Assert
            _mockSender.Verify(x => x.DisposeAsync(), Times.Once);
            _mockClient.Verify(x => x.DisposeAsync(), Times.Once);
        }
    }
}