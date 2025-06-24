using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IssueTicketManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LabelsController : ControllerBase
{
    private readonly ILabelRepository _repository;

    public LabelsController(ILabelRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<ActionResult<Label>> Create(Label label)
    {
        var createdLabel = await _repository.CreateLabelAsync(label);
        return CreatedAtAction(nameof(Get), new { id = createdLabel.Id }, createdLabel);
    }
}