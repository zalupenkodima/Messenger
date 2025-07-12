namespace Messenger.Application.DTOs;

public class ImapNotificationSettings
{
    public bool Enabled { get; set; } = true;
    public int CheckIntervalMinutes { get; set; } = 1;
    public int UnreadThresholdMinutes { get; set; } = 5;
    public SmtpSettings Smtp { get; set; } = new();
    public string FromEmail { get; set; } = "noreply@messenger.com";
    public string FromName { get; set; } = "Messenger Notifications";
}

public class SmtpSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
} 