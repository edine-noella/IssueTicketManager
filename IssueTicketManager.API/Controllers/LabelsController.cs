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
        return CreatedAtAction(nameof(GetById), new { id = createdLabel.Id }, createdLabel);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Label>> GetById(int id) 
    {
        var label = await _repository.GetLabelByIdAsync(id);
        return label != null ? Ok(label) : NotFound();
    }
    
    
}