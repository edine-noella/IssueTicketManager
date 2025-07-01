using IssueTicketManager.API.DTOs;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IssueTicketManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IssuesController : ControllerBase
    {
      private readonly IIssueRepository _repository;
      private readonly IUserRepository _userRepository;

        public IssuesController(IIssueRepository repository, IUserRepository userRepository)
        {
            _repository = repository;
            _userRepository = userRepository;
 }

        [HttpPost]
        public async Task<ActionResult<Issue>> Create(CreateIssueDto dto)
        {
            var issue = new Issue
            {
                Title = dto.Title,
                Body = dto.Body,
                CreatorId = dto.CreatorId,
                IssueLabels = dto.LabelIds.Select(id => new IssueLabel { LabelId = id }).ToList()
            };

            var createdIssue = await _repository.CreateIssueAsync(issue);
            return CreatedAtAction(nameof(GetIssue),  new { id = createdIssue.Id }, createdIssue);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateIssueDto dto)
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
            return Ok(updatedIssue);
           
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
                return Ok(updatedIssue);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error assigning issue: {ex.Message}");
            }
        }


        [HttpPost("{id}/label")]
        public async Task<IActionResult> AddLabelToIssue(int id, [FromBody] AddLabelToIssueDto dto)
        {
            // Check if that issue exists
            var success = await _repository.AddLabelToIssueAsync(id, dto.LabelId);

            if (success == LabelAddResult.IssueNotFound) return NotFound($"Issue with id {id} not found");

            if (success == LabelAddResult.LabelNotFound) return NotFound($"Label with id {dto.LabelId} not found");

            if (success == LabelAddResult.AlreadyAssigned)
                return BadRequest("Label is already assigned to this issue.");

            return NoContent();
        }


        [HttpPost("{id}/comments")]
        public async Task<ActionResult<Comment>> AddCommentToIssue(int id, [FromBody] AddCommentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            //check if an issue exists
            var issueExists = await _repository.IssueExistsAsync(id);
            if (!issueExists) return NotFound($"Issue with ID {id} not found.");
            
            // check if a user exists
            var userExists = await _userRepository.UserExists(dto.UserId);
            if (!userExists) return NotFound($"User with ID {dto.UserId} not found.");
            
            // Create and save comment
            var comment = new Comment
            {
                Text = dto.Text,
                UserId = dto.UserId,
                IssueId = id
            };

            var createdComment = await _repository.AddCommentAsync(comment);
            createdComment = await _repository.GetCommentWithDetailsAsync(createdComment.Id);

            return CreatedAtAction(nameof(GetComment), new { id = createdComment.Id }, createdComment);

        }

        [HttpGet("comment/{id}")]
        public async Task<ActionResult<Comment>> GetComment(int id)
        {
            var comment = await _repository.GetCommentWithDetailsAsync(id);
            return comment != null ? Ok(comment) : NotFound();
        }
        
    }
}