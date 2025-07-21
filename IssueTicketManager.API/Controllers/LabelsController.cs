using System.Linq.Expressions;
using IssueTicketManager.API.DTOs;
using IssueTicketManager.API.Messages;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using IssueTicketManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IssueTicketManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LabelsController : ControllerBase
{
    private readonly ILabelRepository _repository;
    private readonly IServiceBusService _serviceBusService;
    private readonly ILogger<LabelsController> _logger;

    public LabelsController(ILabelRepository repository,  IServiceBusService serviceBusService, ILogger<LabelsController> logger)
    {
        _repository = repository;
        _serviceBusService = serviceBusService;
        _logger = logger;
        
    }

    [HttpPost]
    public async Task<ActionResult<Label>> Create(CreateLabelDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState); 
        }

        try
        {
            var label = new Label
            {
                Name = dto.Name,
                Color = dto.Color,
            };


            var createdLabel = await _repository.CreateLabelAsync(label);
            
            // Publish message to Service Bus
            var message = new LabelCreatedMessage
            {
                LabelId = createdLabel.Id,
                Name = createdLabel.Name, 
                Color = createdLabel.Color,
            };

            await _serviceBusService.PublishLabelCreatedAsync(message);
            
            return CreatedAtAction(nameof(GetLabelById), new { id = createdLabel.Id }, createdLabel);

        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
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