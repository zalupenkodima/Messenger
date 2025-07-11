using Microsoft.AspNetCore.SignalR;
using Messenger.Application.Interfaces;
using Messenger.Shared;
using System.Security.Claims;

namespace Messenger.API.Hubs;

public class ChatHub(
    IMessageService messageService,
    IChatService chatService,
    IUserService userService,
    ILogger<ChatHub> logger) : Hub
{
    private readonly IMessageService _messageService = messageService;
    private readonly IChatService _chatService = chatService;
    private readonly IUserService _userService = userService;
    private readonly ILogger<ChatHub> _logger = logger;
    private static readonly Dictionary<string, Guid> _userConnections = [];

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR connection attempt. ConnectionId: {ConnectionId}, User: {User}", 
            Context.ConnectionId, Context.User?.Identity?.Name);
        
        var userId = GetUserIdFromContext();
        if (userId.HasValue)
        {
            _userConnections[Context.ConnectionId] = userId.Value;
            await _userService.UpdateOnlineStatusAsync(userId.Value, true);
            
            var userChats = await _chatService.GetUserChatsAsync(userId.Value);
            _logger.LogInformation("User {UserId} has {ChatCount} chats", userId.Value, userChats.Count());
            
            foreach (var chat in userChats)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, chat.Id.ToString());
                _logger.LogInformation("User {UserId} joined chat group {ChatId}", userId.Value, chat.Id);
            }
            
            // Уведомляем всех участников чатов о том, что пользователь онлайн
            await NotifyUserOnlineStatusChange(userId.Value, true);
        }
        else
        {
            _logger.LogWarning("SignalR connection without valid user ID. ConnectionId: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserIdFromContext();
        if (userId.HasValue)
        {
            _userConnections.Remove(Context.ConnectionId);
            await _userService.UpdateOnlineStatusAsync(userId.Value, false);
            
            // Уведомляем всех участников чатов о том, что пользователь оффлайн
            await NotifyUserOnlineStatusChange(userId.Value, false);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<MessageDto> SendMessage(CreateMessageDto messageDto)
    {
        var userId = GetUserIdFromContext();
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("User not authenticated");

        _logger.LogInformation("User {UserId} trying to send message to chat {ChatId}", userId.Value, messageDto.ChatId);
        _logger.LogInformation("SendMessage - UserId type: {UserIdType}, Value: {UserIdValue}", userId.Value.GetType().Name, userId.Value);

        try
        {
            var message = await _messageService.SendMessageAsync(messageDto, userId.Value);
            _logger.LogInformation("Message sent successfully: {MessageId} with SenderId: {SenderId}", message.Id, message.SenderId);
            await Clients.Group(messageDto.ChatId.ToString()).SendAsync("MessageReceived", message);
            _logger.LogInformation("Message broadcasted to chat {ChatId}", messageDto.ChatId);
            
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message: {Message}", ex.Message);
            throw;
        }
    }

    public async Task UpdateMessage(Guid messageId, UpdateMessageDto updateMessageDto)
    {
        var userId = GetUserIdFromContext();
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("User not authenticated");

        var message = await _messageService.UpdateMessageAsync(messageId, updateMessageDto, userId.Value);
        
        await Clients.Group(message.ChatId.ToString()).SendAsync("MessageUpdated", message);
    }

    public async Task DeleteMessage(Guid messageId)
    {
        var userId = GetUserIdFromContext();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var message = await _messageService.GetMessageAsync(messageId);
        if (message == null)
            throw new InvalidOperationException("Message not found");

        await _messageService.DeleteMessageAsync(messageId, userId.Value);
        
        await Clients.Group(message.ChatId.ToString()).SendAsync("MessageDeleted", messageId);
    }

    public async Task JoinChat(Guid chatId)
    {
        var userId = GetUserIdFromContext();
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("User not authenticated");

        var chat = await _chatService.GetChatAsync(chatId, userId.Value);
        if (chat == null)
            throw new InvalidOperationException("Chat not found or access denied");

        await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());
        await Clients.Group(chatId.ToString()).SendAsync("UserJoinedChat", userId.Value);
    }

    public async Task LeaveChat(Guid chatId)
    {
        var userId = GetUserIdFromContext();
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("User not authenticated");

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());
        await Clients.Group(chatId.ToString()).SendAsync("UserLeftChat", userId.Value);
    }

    public async Task Typing(Guid chatId, bool isTyping)
    {
        var userId = GetUserIdFromContext();
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("User not authenticated");

        await Clients.Group(chatId.ToString()).SendAsync("UserTyping", userId.Value, isTyping);
    }

    private async Task NotifyUserOnlineStatusChange(Guid userId, bool isOnline)
    {
        try
        {
            var userChats = await _chatService.GetUserChatsAsync(userId);
            foreach (var chat in userChats)
            {
                await Clients.Group(chat.Id.ToString()).SendAsync("UserOnlineStatusChanged", userId, isOnline);
                _logger.LogInformation("Notified chat {ChatId} about user {UserId} online status: {IsOnline}", 
                    chat.Id, userId, isOnline);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying about user online status change for user {UserId}", userId);
        }
    }

    private Guid? GetUserIdFromContext()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("GetUserIdFromContext - UserIdClaim: {UserIdClaim}", userIdClaim);
        _logger.LogInformation("GetUserIdFromContext - All claims: {Claims}", 
            string.Join(", ", Context.User?.Claims.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>()));
        
        if (userIdClaim != null)
        {
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogInformation("User ID extracted from claims: {UserId}", userId);
                return userId;
            }
            else
            {
                _logger.LogWarning("Invalid user ID format in claims: {UserIdClaim}", userIdClaim);
            }
        }
        else
        {
            _logger.LogWarning("No user ID claim found. Available claims: {Claims}", 
                string.Join(", ", Context.User?.Claims.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>()));
        }
        return null;
    }
} 