using FluentAssertions;
using IssueTicketManager.API.Controllers;
using IssueTicketManager.API.DTOs;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IssueTicketManager.Tests.ControllersTests;

[TestFixture]
public class LabelControllerTests
{
      private Mock<ILabelRepository> _mockRepository;
        private LabelsController _controller;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new Mock<ILabelRepository>();
            _controller = new LabelsController(_mockRepository.Object);
        }

        [Test]
        public async Task Create_WithValidLabel_ReturnsCreatedAtAction()
        {
            // Arrange
            var labelDto = new CreateLabelDto 
            { 
                Name = "goodFirstIssue",
                Color = "#FF0000" 
            };
    
            var expectedLabel = new Label { Id = 1, Name = labelDto.Name, Color = labelDto.Color };
    
            _mockRepository.Setup(x => x.CreateLabelAsync(It.Is<Label>(l => 
                    l.Name == labelDto.Name && 
                    l.Color == labelDto.Color))).ReturnsAsync(expectedLabel);

            // Act
            var result = await _controller.Create(labelDto);

            // Assert
            var createdAtActionResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdAtActionResult.ActionName.Should().Be(nameof(_controller.GetLabelById));
            createdAtActionResult.Value.Should().BeEquivalentTo(expectedLabel);
    
            _mockRepository.Verify(x => x.CreateLabelAsync(It.Is<Label>(l => 
                    l.Name == labelDto.Name && l.Color == labelDto.Color)), Times.Once);
            
        }

        [Test]
        public async Task Create_WithInvalidLabel_ReturnsValidationProblem()
        {
            // Arrange
            var invalidLabelDto = new CreateLabelDto 
            { 
                Name = null,
                Color = "#FF0000" 
            };
            _controller.ModelState.AddModelError("Name", "Required");

            // Act
            var result = await _controller.Create(invalidLabelDto);

            // Assert
            var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.Value.Should().BeOfType<ValidationProblemDetails>()
                .Which.Errors.Should().ContainKey("Name");

        }

        [Test]
        public async Task GetLabelById_WithExistingId_ReturnsLabel()
        {
            // Arrange
            var labelId = 1;
            var expectedLabel = new Label { Id = labelId, Name = "Bug" };
            
            _mockRepository.Setup(x => x.GetLabelByIdAsync(labelId))
                .ReturnsAsync(expectedLabel);

            // Act
            var result = await _controller.GetLabelById(labelId);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeEquivalentTo(expectedLabel);
        }

        [Test]
        public async Task GetLabelById_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            var labelId = 999;
            _mockRepository.Setup(x => x.GetLabelByIdAsync(labelId))
                .ReturnsAsync((Label)null);

            // Act
            var result = await _controller.GetLabelById(labelId);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Test]
        public async Task GetAll_WhenCalled_ReturnsAllLabels()
        {
            // Arrange
            var labels = new List<Label>
            {
                new Label { Id = 1, Name = "Bug" },
                new Label { Id = 2, Name = "Feature" }
            };
            
            _mockRepository.Setup(x => x.GetAllLabelsAsync())
                .ReturnsAsync(labels);

            // Act
            var result = await _controller.GetAll();

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeEquivalentTo(labels);
        }

        [Test]
        public async Task GetAll_WhenNoLabelsExist_ReturnsEmptyList()
        {
            // Arrange
            _mockRepository.Setup(x => x.GetAllLabelsAsync())
                .ReturnsAsync(new List<Label>());

            // Act
            var result = await _controller.GetAll();
 
            // Assert
            
            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.As<List<Label>>().Should().BeEmpty();
        }
        
        [Test]
        public async Task Create_WithDuplicateLabelName_ReturnsConflict()
        {
            // Arrange
            var duplicateName = "existingLabel";
            var labelDto = new CreateLabelDto 
            { 
                Name = duplicateName,
                Color = "#FF0000"
            };

            _mockRepository.Setup(x => x.CreateLabelAsync(It.Is<Label>(l => l.Name == duplicateName)))
                .ThrowsAsync(new InvalidOperationException("Label name already exists"));

            // Act
            var result = await _controller.Create(labelDto);

            // Assert
            result.Result.Should().BeOfType<ConflictObjectResult>();
            var conflictResult = result.Result as ConflictObjectResult;
            conflictResult.Value.Should().BeEquivalentTo(new { error = "Label name already exists" });
    
            _mockRepository.Verify(x => x.CreateLabelAsync(It.Is<Label>(l => 
                    l.Name == duplicateName && 
                    l.Color == labelDto.Color)), 
                Times.Once);
        }
        
        [Test]
        public async Task Create_WithInvalidColor_ReturnsValidationProblem()
        {
            // Arrange
            var invalidLabel = new CreateLabelDto 
            { 
                Name = "validName",
                Color = "not-a-color" 
            };

            _controller.ModelState.AddModelError("Color", "Color must be a valid hex color (e.g., #FF0000 or #F00)");

            // Act
            var result = await _controller.Create(invalidLabel);

            // Assert
            var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.Value.Should().BeOfType<ValidationProblemDetails>()
                .Which.Errors.Should().ContainKey("Color");
        }
}