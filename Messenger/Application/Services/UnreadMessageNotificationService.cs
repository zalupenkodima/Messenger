using Microsoft.Extensions.Options;
using Messenger.Application.Interfaces;
using Messenger.Application.DTOs;
using Messenger.Domain.Interfaces;
using Messenger.Domain.Entities;

namespace Messenger.Application.Services;

public class UnreadMessageNotificationService(
    IServiceProvider serviceProvider,
    IOptions<ImapNotificationSettings> settings,
    ILogger<UnreadMessageNotificationService> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ImapNotificationSettings _settings = settings.Value;
    private readonly ILogger<UnreadMessageNotificationService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Unread message notification service is disabled");
            return;
        }

        _logger.LogInformation("Starting unread message notification service. Check interval: {Interval} minutes", _settings.CheckIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckUnreadMessagesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking unread messages");
                
                await Task.Delay(TimeSpan.FromMinutes(Math.Max(_settings.CheckIntervalMinutes, 5)), stoppingToken);
                continue;
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.CheckIntervalMinutes), stoppingToken);
        }
    }

    private async Task CheckUnreadMessagesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            var messageRepository = scope.ServiceProvider.GetRequiredService<IMessageRepository>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var chatRepository = scope.ServiceProvider.GetRequiredService<IChatRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var thresholdTime = DateTime.UtcNow.AddMinutes(-_settings.UnreadThresholdMinutes);
            
            _logger.LogDebug("Checking for unread messages older than {ThresholdTime}", thresholdTime);

            var oldMessages = await messageRepository.GetMessagesOlderThanAsync(thresholdTime);
            
            _logger.LogDebug("Found {MessageCount} old messages to check", oldMessages.Count());
            
            foreach (var message in oldMessages)
            {
                try
                {
                    await ProcessMessageForNotificationsAsync(message, chatRepository, userRepository, notificationService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message {MessageId} for notifications", message.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckUnreadMessagesAsync");
            throw;
        }
    }

    private async Task ProcessMessageForNotificationsAsync(
        Message message, 
        IChatRepository chatRepository, 
        IUserRepository userRepository, 
        INotificationService notificationService)
    {
        try
        {
            var chatMembers = await chatRepository.GetChatMembersAsync(message.ChatId);
            var chat = await chatRepository.GetByIdAsync(message.ChatId);
            var sender = await userRepository.GetByIdAsync(message.SenderId);

            if (chat == null || sender == null)
            {
                _logger.LogWarning("Chat or sender not found for message {MessageId}", message.Id);
                return;
            }

            foreach (var member in chatMembers)
            {
                try
                {
                    if (member.UserId == message.SenderId)
                        continue;

                    if (member.LastReadMessageAt.HasValue && member.LastReadMessageAt.Value >= message.CreatedAt)
                        continue;

                    if (await notificationService.IsNotificationSentAsync(message.Id, member.UserId))
                        continue;

                    var user = await userRepository.GetByIdAsync(member.UserId);
                    if (user == null || string.IsNullOrEmpty(user.Email))
                    {
                        _logger.LogWarning("User {UserId} not found or has no email", member.UserId);
                        continue;
                    }
                    await notificationService.SendUnreadMessageNotificationAsync(
                        user.Email,
                        user.Username,
                        sender.Username,
                        chat.Name,
                        message.Content,
                        message.CreatedAt
                    );

                    await notificationService.MarkNotificationAsSentAsync(message.Id, member.UserId);

                    _logger.LogInformation("Sent notification to user {UserId} for message {MessageId} in chat {ChatId}", 
                        member.UserId, message.Id, message.ChatId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing member {MemberId} for message {MessageId}", member.UserId, message.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId} for notifications", message.Id);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping unread message notification service");
        await base.StopAsync(cancellationToken);
    }
} 