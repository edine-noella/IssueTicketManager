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
        ModelState.Remove("IssueLabels");
    
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var createdLabel = await _repository.CreateLabelAsync(label);
        return CreatedAtAction(nameof(GetLabelById), new { id = createdLabel.Id }, createdLabel);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Label>> GetLabelById(int id) 
    {
        var label = await _repository.GetLabelByIdAsync(id);
        return label != null ? Ok(label) : NotFound();
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Label>>> GetAll()
    {
        return Ok(await _repository.GetAllLabelsAsync());
    }
    
    
}