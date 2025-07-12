using System.Net.Mail;
using System.Net;
using Messenger.Application.Interfaces;
using Messenger.Application.DTOs;
using Microsoft.Extensions.Options;
using Messenger.Infrastructure.Services;

namespace Messenger.Application.Services;

public class NotificationService(
    IOptions<ImapNotificationSettings> settings,
    ILogger<NotificationService> logger,
    IRedisService redisService) : INotificationService
{
    private readonly ImapNotificationSettings _settings = settings.Value;
    private readonly ILogger<NotificationService> _logger = logger;
    private readonly IRedisService _redisService = redisService;
    private readonly HashSet<string> _sentNotifications = [];

    public async Task SendUnreadMessageNotificationAsync(string userEmail, string username, string senderName, string chatName, string messageContent, DateTime messageTime)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Email notifications are disabled");
            return;
        }

        if (string.IsNullOrEmpty(_settings.Smtp.Username) || string.IsNullOrEmpty(_settings.Smtp.Password))
        {
            _logger.LogWarning("SMTP credentials are not configured. Please set Username and Password in appsettings.json");
            return;
        }

        if (string.IsNullOrEmpty(_settings.Smtp.Host))
        {
            _logger.LogWarning("SMTP host is not configured");
            return;
        }

        try
        {
            var notificationKey = $"notification:{userEmail}:{messageTime:yyyyMMddHHmmss}";
            
            bool notificationExists = false;
            try
            {
                notificationExists = await _redisService.KeyExistsAsync(notificationKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis connection failed, using in-memory fallback");
                notificationExists = _sentNotifications.Contains(notificationKey);
            }

            if (notificationExists)
            {
                _logger.LogInformation("Notification already sent for user {UserEmail} at {MessageTime}", userEmail, messageTime);
                return;
            }

            using var client = new SmtpClient(_settings.Smtp.Host, _settings.Smtp.Port)
            {
                EnableSsl = _settings.Smtp.EnableSsl,
                Credentials = new NetworkCredential(_settings.Smtp.Username, _settings.Smtp.Password),
                Timeout = 10000
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = $"Непрочитанное сообщение в чате '{chatName}'",
                Body = CreateEmailBody(username, senderName, chatName, messageContent, messageTime),
                IsBodyHtml = true
            };

            mailMessage.To.Add(userEmail);

            _logger.LogInformation("Attempting to send email notification to {UserEmail} via {SmtpHost}:{SmtpPort}", 
                userEmail, _settings.Smtp.Host, _settings.Smtp.Port);

            await client.SendMailAsync(mailMessage);
            
            try
            {
                await _redisService.SetAsync(notificationKey, new { SentAt = DateTime.UtcNow }, TimeSpan.FromHours(24));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save notification to Redis, using in-memory fallback");
                _sentNotifications.Add(notificationKey);
            }

            _logger.LogInformation("Email notification sent successfully to {UserEmail} for message from {SenderName} in chat {ChatName}", 
                userEmail, senderName, chatName);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email notification to {UserEmail}. StatusCode: {StatusCode}, Message: {Message}", 
                userEmail, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification to {UserEmail}", userEmail);
        }
    }

    public async Task<bool> IsNotificationSentAsync(Guid messageId, Guid userId)
    {
        var notificationKey = $"notification:{messageId}:{userId}";
        try
        {
            return await _redisService.KeyExistsAsync(notificationKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis connection failed, checking in-memory fallback");
            return _sentNotifications.Contains(notificationKey);
        }
    }

    public async Task MarkNotificationAsSentAsync(Guid messageId, Guid userId)
    {
        var notificationKey = $"notification:{messageId}:{userId}";
        try
        {
            await _redisService.SetAsync(notificationKey, new { SentAt = DateTime.UtcNow }, TimeSpan.FromHours(24));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save notification to Redis, using in-memory fallback");
            _sentNotifications.Add(notificationKey);
        }
    }

    private static string CreateEmailBody(string username, string senderName, string chatName, string messageContent, DateTime messageTime)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 20px; border-radius: 0 0 5px 5px; }}
        .message-box {{ background-color: white; padding: 15px; border-left: 4px solid #007bff; margin: 15px 0; }}
        .time {{ color: #666; font-size: 0.9em; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 0.8em; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Непрочитанное сообщение</h1>
        </div>
        <div class='content'>
            <p>Здравствуйте, <strong>{username}</strong>!</p>
            <p>У вас есть непрочитанное сообщение в чате <strong>'{chatName}'</strong>.</p>
            
            <div class='message-box'>
                <p><strong>От:</strong> {senderName}</p>
                <p><strong>Сообщение:</strong></p>
                <p>{messageContent}</p>
                <p class='time'>Отправлено: {messageTime:dd.MM.yyyy HH:mm}</p>
            </div>
            
            <p>Пожалуйста, войдите в приложение, чтобы прочитать сообщение.</p>
        </div>
        <div class='footer'>
            <p>Это автоматическое уведомление от Messenger</p>
        </div>
    </div>
</body>
</html>";
    }
} 