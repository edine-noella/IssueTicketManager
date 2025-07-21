using IssueTicketManager.API.DTOs;
using IssueTicketManager.API.Messages;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using IssueTicketManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IssueTicketManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IssuesController : ControllerBase
    {
        private readonly IIssueRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IServiceBusService _serviceBusService;
        private readonly ILogger<IssuesController> _logger;

        public IssuesController(
            IIssueRepository repository, 
            IUserRepository userRepository,
            IServiceBusService serviceBusService,
            ILogger<IssuesController> logger)
        {
            _repository = repository;
            _userRepository = userRepository;
            _serviceBusService = serviceBusService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<Issue>> Create(CreateIssueDto dto)
        {
            try
            {
                var issue = new Issue
                {
                    Title = dto.Title,
                    Body = dto.Body,
                    CreatorId = dto.CreatorId,
                    IssueLabels = dto.LabelIds.Select(id => new IssueLabel { LabelId = id }).ToList()
                };

                var createdIssue = await _repository.CreateIssueAsync(issue);

                // Publish message to Service Bus
                var message = new IssueCreatedMessage
                {
                    IssueId = createdIssue.Id,
                    Title = createdIssue.Title,
                    Body = createdIssue.Body,
                    CreatorId = createdIssue.CreatorId,
                    LabelIds = dto.LabelIds.ToList()
                };

                await _serviceBusService.PublishIssueCreatedAsync(message);

                return CreatedAtAction(nameof(GetIssue), new { id = createdIssue.Id }, createdIssue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating issue");
                return StatusCode(500, "An error occurred while creating the issue");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateIssueDto dto)
        {
            try
            {
                var issue = await _repository.GetIssueByIdAsync(id);
                if (issue == null) return NotFound();

                if (dto.Title != null) issue.Title = dto.Title;
                if (dto.Body != null) issue.Body = dto.Body;
                if (dto.Status.HasValue) issue.Status = dto.Status.Value;
                if (dto.AssigneeId.HasValue) issue.AssigneeId = dto.AssigneeId;

                if (dto.LabelIds != null)
                {
                    issue.IssueLabels = dto.LabelIds
                        .Select(labelId => new IssueLabel { IssueId = id, LabelId = labelId })
                        .ToList();
                }

                var updatedIssue = await _repository.UpdateIssueAsync(issue);

                // Publish message to Service Bus
                var message = new IssueUpdatedMessage
                {
                    IssueId = id,
                    Title = dto.Title,
                    Body = dto.Body,
                    Status = dto.Status?.ToString(),
                    AssigneeId = dto.AssigneeId,
                    LabelIds = dto.LabelIds?.ToList()
                };

                await _serviceBusService.PublishIssueUpdatedAsync(message);

                return Ok(updatedIssue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating issue {IssueId}", id);
                return StatusCode(500, "An error occurred while updating the issue");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Issue>> GetIssue(int id)
        {
            var issue = await _repository.GetIssueByIdAsync(id);
            return issue != null ? Ok(issue) : NotFound();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Issue>>> GetIssues()
        {
            return Ok(await _repository.GetAllIssuesAsync());
        }

        [HttpPatch("{id}/assign")]
        public async Task<ActionResult<Issue>> AssignIssue(int id, [FromBody] AssignUserIssueDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var issue = await _repository.GetIssueByIdAsync(id);
                if (issue == null) return NotFound("Issue not found");

                var userExists = await _userRepository.UserExists(dto.AssigneeId.Value);
                if (!userExists) return BadRequest("Assignee not found");

                issue.AssigneeId = dto.AssigneeId;

                var updatedIssue = await _repository.UpdateIssueAsync(issue);

                // Publish message to Service Bus
                var message = new IssueAssignedMessage
                {
                    IssueId = id,
                    AssigneeId = dto.AssigneeId.Value,
                    AssignedByUserId = 1 // You would get this from the current user context
                };

                await _serviceBusService.PublishIssueAssignedAsync(message);

                return Ok(updatedIssue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning issue {IssueId}", id);
                return StatusCode(500, $"Error assigning issue: {ex.Message}");
            }
        }

        [HttpPost("{id}/label")]
        public async Task<IActionResult> AddLabelToIssue(int id, [FromBody] AddLabelToIssueDto dto)
        {
            try
            {
                var (updatedIssue, result) = await _repository.AddLabelToIssueAsync(id, dto.LabelId);

                switch (result)
                {
                    case LabelAddResult.IssueNotFound:
                        return NotFound($"Issue with ID {id} not found.");

                    case LabelAddResult.LabelNotFound:
                        return NotFound($"Label with ID {dto.LabelId} not found.");

                    case LabelAddResult.AlreadyAssigned:
                        return BadRequest("The label is already assigned to this issue.");

                    case LabelAddResult.Success:
                        // Publish message to Service Bus
                        var message = new IssueLabelAssignedMessage
                        {
                            IssueId = id,
                            LabelId = dto.LabelId,
                            AssignedByUserId = 1 // You would get this from the current user context
                        };

                        await _serviceBusService.PublishIssueLabelAssignedAsync(message);

                        return Ok(updatedIssue);

                    default:
                        return StatusCode(500, "An unknown error occurred.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding label {LabelId} to issue {IssueId}", dto.LabelId, id);
                return StatusCode(500, "An error occurred while adding the label to the issue");
            }
        }
    }
}