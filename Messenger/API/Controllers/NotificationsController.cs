using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Messenger.Application.DTOs;
using Messenger.Application.Interfaces;
using System.Security.Claims;

namespace Messenger.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController(
    INotificationService notificationService,
    IOptions<ImapNotificationSettings> settings,
    ILogger<NotificationsController> logger) : ControllerBase
{
    private readonly INotificationService _notificationService = notificationService;
    private readonly IOptions<ImapNotificationSettings> _settings = settings;
    private readonly ILogger<NotificationsController> _logger = logger;

    [HttpGet("settings")]
    public ActionResult<ImapNotificationSettings> GetSettings()
    {
        var settings = _settings.Value;

        var safeSettings = new ImapNotificationSettings
        {
            Enabled = settings.Enabled,
            CheckIntervalMinutes = settings.CheckIntervalMinutes,
            UnreadThresholdMinutes = settings.UnreadThresholdMinutes,
            FromEmail = settings.FromEmail,
            FromName = settings.FromName,
            Smtp = new SmtpSettings
            {
                Host = settings.Smtp.Host,
                Port = settings.Smtp.Port,
                EnableSsl = settings.Smtp.EnableSsl,
                Username = settings.Smtp.Username,
                Password = "***"
            }
        };

        return Ok(safeSettings);
    }

    [HttpPost("test")]
    public async Task<IActionResult> SendTestNotification()
    {
        try
        {
            var userId = GetUserIdFromClaims();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            await _notificationService.SendUnreadMessageNotificationAsync(
                "test@example.com",
                "TestUser",
                "System",
                "Test Chat",
                "Это тестовое уведомление для проверки работы системы.",
                DateTime.UtcNow
            );

            return Ok(new { message = "Тестовое уведомление отправлено" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test notification");
            return StatusCode(500, new { error = "Ошибка отправки тестового уведомления" });
        }
    }

    private Guid? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
} 