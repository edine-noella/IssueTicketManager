using IssueTicketManager.API.DTOs;
using IssueTicketManager.API.Messages;
using IssueTicketManager.API.Models;
using IssueTicketManager.API.Repositories.Interfaces;
using IssueTicketManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace IssueTicketManager.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CommentController: ControllerBase
{
    private readonly ICommentRepository _repository;
    private readonly IIssueRepository _issueRepository;
    private readonly IUserRepository _userRepository;
    private readonly IServiceBusService _serviceBusService;
    private readonly ILogger<CommentController> _logger;
    public CommentController(
        ICommentRepository repository,
        IIssueRepository issueRepository,
        IUserRepository userRepository,
        IServiceBusService serviceBusService,
        ILogger<CommentController> logger
        )
    {
        _repository = repository;
        _issueRepository = issueRepository;
        _userRepository = userRepository;
        _serviceBusService = serviceBusService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> AddComment([FromBody] AddCommentDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var issueExists = await _issueRepository.IssueExistsAsync(dto.IssueId);
        if(issueExists == false) return NotFound("Issue not found");
        
        var userExists = await _userRepository.UserExists(dto.UserId);
        if(userExists == false) return NotFound("User not found");
        
        // Create and save comment
        var newComment = new Comment
        {
            Text = dto.Text,
            UserId = dto.UserId,
            IssueId = dto.IssueId
        };

        var comment = await _repository.AddCommentAsync(newComment);
        
        // Publish message to Service Bus
        var message = new IssueCommentCreatedMessage
        {
            CommentId = comment.Id, 
            IssueId = comment.IssueId, 
            UserId = comment.UserId, 
            Content = comment.Text,
        };

        await _serviceBusService.PublishIssueCommentCreatedAsync(message);
        
        comment = await _repository.GetCommentWithDetailsAsync(comment.Id);

        return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, comment);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetComment(int id)
    {
        var comment = await _repository.GetCommentWithDetailsAsync(id);
       return comment != null ? Ok(comment) : NotFound();
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAllComments()
    {
        return Ok(await _repository.GetAllCommentsAsync());
    }
}