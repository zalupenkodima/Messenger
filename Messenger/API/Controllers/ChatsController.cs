using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Messenger.Application.Interfaces;
using Messenger.Shared;

namespace Messenger.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatsController(IChatService chatService) : ControllerBase
{
    private readonly IChatService _chatService = chatService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChatDto>>> GetUserChats()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var chats = await _chatService.GetUserChatsAsync(userId.Value);
        return Ok(chats);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChatDto>> GetChat(Guid id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var chat = await _chatService.GetChatAsync(id, userId.Value);
        if (chat == null)
        {
            return NotFound();
        }

        return Ok(chat);
    }

    [HttpPost]
    public async Task<ActionResult<ChatDto>> CreateChat(CreateChatDto createChatDto)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var chat = await _chatService.CreateChatAsync(createChatDto, userId.Value);
            return CreatedAtAction(nameof(GetChat), new { id = chat.Id }, chat);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ChatDto>> UpdateChat(Guid id, UpdateChatDto updateChatDto)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var chat = await _chatService.UpdateChatAsync(id, updateChatDto, userId.Value);
            return Ok(chat);
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

    [HttpPost("{id}/members")]
    public async Task<ActionResult> AddMember(Guid id, [FromBody] AddMemberDto addMemberDto)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            await _chatService.AddMemberToChatAsync(id, addMemberDto.MemberId, userId.Value);
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

    [HttpDelete("{id}/members/{memberId}")]
    public async Task<ActionResult> RemoveMember(Guid id, Guid memberId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            await _chatService.RemoveMemberFromChatAsync(id, memberId, userId.Value);
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

    [HttpPost("{id}/leave")]
    public async Task<ActionResult> LeaveChat(Guid id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            await _chatService.LeaveChatAsync(id, userId.Value);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteChat(Guid id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            await _chatService.DeleteChatAsync(id, userId.Value);
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

    [HttpPost("{id}/mark-as-read")]
    public async Task<ActionResult> MarkChatAsRead(Guid id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            await _chatService.MarkChatAsReadAsync(id, userId.Value);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }
}

public class AddMemberDto
{
    public Guid MemberId { get; set; }
} 