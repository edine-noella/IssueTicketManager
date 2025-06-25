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

        public IssuesController(IIssueRepository repository)
        {
            _repository = repository;
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

            await _repository.UpdateIssueAsync(issue);
            return NoContent();
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
    }
}