using Microsoft.AspNetCore.Mvc;
using Messenger.Application.Interfaces;
using Messenger.Shared;
using Microsoft.Extensions.Logging;

namespace Messenger.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    
    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<UserDto>>> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Query is required");
            
        _logger.LogInformation("Search request received for query: {Query}", query);
        var users = await _userService.SearchUsersAsync(query);
        _logger.LogInformation("Search completed. Returning {Count} users", users.Count());
        
        return Ok(users);
    }
    
    [HttpGet("online")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetOnlineUsers()
    {
        var users = await _userService.GetOnlineUsersAsync();
        return Ok(users);
    }
} 