using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Messenger.Application.Interfaces;
using Messenger.Shared;

namespace Messenger.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MessagesController(IMessageService messageService) : ControllerBase
{
    private readonly IMessageService _messageService = messageService;

    [HttpGet("chat/{chatId}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetChatMessages(
        Guid chatId, 
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 50)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var messages = await _messageService.GetChatMessagesAsync(chatId, userId.Value, skip, take);
            return Ok(messages);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MessageDto>> GetMessage(Guid id)
    {
        var message = await _messageService.GetMessageAsync(id);
        if (message == null)
        {
            return NotFound();
        }

        return Ok(message);
    }

    [HttpGet("{id}/replies")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetReplies(Guid id)
    {
        var replies = await _messageService.GetRepliesAsync(id);
        return Ok(replies);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MessageDto>> UpdateMessage(Guid id, UpdateMessageDto updateMessageDto)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var message = await _messageService.UpdateMessageAsync(id, updateMessageDto, userId.Value);
            return Ok(message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMessage(Guid id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            await _messageService.DeleteMessageAsync(id, userId.Value);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }
} 