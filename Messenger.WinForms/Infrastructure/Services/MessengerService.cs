using Messenger.Client.Interfaces;
using Messenger.Shared;
using Messenger.WinForms.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Messenger.WinForms.Infrastructure.Services;

public class MessengerService : IMessengerService
{
    private readonly IMessengerClient _client;
    private readonly ILogger<MessengerService> _logger;

    public MessengerService(IMessengerClient client, ILogger<MessengerService> logger)
    {
        _client = client;
        _logger = logger;
        
        _client.MessageReceived += OnMessageReceived;
        _client.MessageUpdated += OnMessageUpdated;
        _client.MessageDeleted += OnMessageDeleted;
        _client.UserJoinedChat += OnUserJoinedChat;
        _client.UserLeftChat += OnUserLeftChat;
        _client.UserTyping += OnUserTyping;
        _client.UserOnlineStatusChanged += OnUserOnlineStatusChanged;
    }

    public string? Token => _client.Token;
    public bool IsConnected => _client.IsConnected;

    public async Task<bool> AuthenticateAsync(string username, string password)
    {
        try
        {
            _logger.LogInformation("Attempting to authenticate user: {Username}", username);
            var result = await _client.AuthenticateAsync(username, password);
            if (result)
            {
                _logger.LogInformation("User {Username} authenticated successfully", username);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for user: {Username}", username);
            return false;
        }
    }

    public async Task<bool> RegisterAsync(string username, string email, string password)
    {
        try
        {
            _logger.LogInformation("Attempting to register user: {Username}", username);
            var result = await _client.RegisterAsync(username, email, password);
            if (result)
            {
                _logger.LogInformation("User {Username} registered successfully", username);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for user: {Username}", username);
            return false;
        }
    }

    public void SetToken(string token)
    {
        _client.SetToken(token);
        _logger.LogInformation("Token set for client");
    }

    public async Task ConnectAsync()
    {
        try
        {
            _logger.LogInformation("Connecting to SignalR hub...");
            await _client.ConnectAsync();
            _logger.LogInformation("Successfully connected to SignalR hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR hub");
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _logger.LogInformation("Disconnecting from SignalR hub...");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var disconnectTask = _client.DisconnectAsync();
            
            if (await Task.WhenAny(disconnectTask, Task.Delay(10000, cts.Token)) == disconnectTask)
            {
                await disconnectTask;
                _logger.LogInformation("Successfully disconnected from SignalR hub");
            }
            else
            {
                _logger.LogWarning("Disconnect timeout, continuing with logout process");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect from SignalR hub");
        }
    }

    public async Task<IEnumerable<ChatDto>> GetChatsAsync()
    {
        try
        {
            return await _client.GetChatsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chats");
            return Enumerable.Empty<ChatDto>();
        }
    }

    public async Task<ChatDto?> GetChatAsync(Guid chatId)
    {
        try
        {
            return await _client.GetChatAsync(chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chat: {ChatId}", chatId);
            return null;
        }
    }

    public async Task<ChatDto> CreateChatAsync(CreateChatDto createChatDto)
    {
        try
        {
            return await _client.CreateChatAsync(createChatDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create chat: {ChatName}", createChatDto.Name);
            throw;
        }
    }

    public async Task<ChatDto> UpdateChatAsync(Guid chatId, UpdateChatDto updateChatDto)
    {
        try
        {
            return await _client.UpdateChatAsync(chatId, updateChatDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chat: {ChatId}", chatId);
            throw;
        }
    }

    public async Task AddMemberToChatAsync(Guid chatId, Guid memberId)
    {
        try
        {
            await _client.AddMemberToChatAsync(chatId, memberId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add member {MemberId} to chat {ChatId}", memberId, chatId);
            throw;
        }
    }

    public async Task RemoveMemberFromChatAsync(Guid chatId, Guid memberId)
    {
        try
        {
            await _client.RemoveMemberFromChatAsync(chatId, memberId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove member {MemberId} from chat {ChatId}", memberId, chatId);
            throw;
        }
    }

    public async Task LeaveChatAsync(Guid chatId)
    {
        try
        {
            await _client.LeaveChatAsync(chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave chat: {ChatId}", chatId);
            throw;
        }
    }

    public async Task DeleteChatAsync(Guid chatId)
    {
        try
        {
            await _client.DeleteChatAsync(chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete chat: {ChatId}", chatId);
            throw;
        }
    }

    public async Task MarkChatAsReadAsync(Guid chatId)
    {
        try
        {
            await _client.MarkChatAsReadAsync(chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark chat as read: {ChatId}", chatId);
        }
    }

    public async Task<IEnumerable<MessageDto>> GetChatMessagesAsync(Guid chatId, int skip = 0, int take = 50)
    {
        try
        {
            return await _client.GetChatMessagesAsync(chatId, skip, take);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages for chat: {ChatId}", chatId);
            return [];
        }
    }

    public async Task<MessageDto?> GetMessageAsync(Guid messageId)
    {
        try
        {
            return await _client.GetMessageAsync(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message: {MessageId}", messageId);
            return null;
        }
    }

    public async Task<IEnumerable<MessageDto>> GetRepliesAsync(Guid messageId)
    {
        try
        {
            return await _client.GetRepliesAsync(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get replies for message: {MessageId}", messageId);
            return [];
        }
    }

    public async Task<MessageDto> SendMessageAsync(CreateMessageDto createMessageDto)
    {
        try
        {
            return await _client.SendMessageAsync(createMessageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to chat: {ChatId}", createMessageDto.ChatId);
            throw;
        }
    }

    public async Task<MessageDto> UpdateMessageAsync(Guid messageId, UpdateMessageDto updateMessageDto)
    {
        try
        {
            return await _client.UpdateMessageAsync(messageId, updateMessageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update message: {MessageId}", messageId);
            throw;
        }
    }

    public async Task DeleteMessageAsync(Guid messageId)
    {
        try
        {
            await _client.DeleteMessageAsync(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message: {MessageId}", messageId);
            throw;
        }
    }

    public async Task<IEnumerable<UserDto>> SearchUsersAsync(string query)
    {
        try
        {
            _logger.LogInformation("Searching users with query: {Query}", query);
            var users = await _client.SearchUsersAsync(query);
            _logger.LogInformation("Found {Count} users", users.Count());
            
            foreach (var user in users)
            {
                _logger.LogInformation("User found: {Username} ({Email}) - Online: {IsOnline}", 
                    user.Username, user.Email, user.IsOnline);
            }
            
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search users with query: {Query}", query);
            return [];
        }
    }

    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        try
        {
            return await _client.GetUserAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user: {UserId}", userId);
            return null;
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            return await _client.GetCurrentUserAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user");
            return null;
        }
    }

    public async Task<IEnumerable<UserDto>> GetOnlineUsersAsync()
    {
        try
        {
            return await _client.GetOnlineUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get online users");
            return [];
        }
    }

    public async Task SendTypingIndicatorAsync(Guid chatId, bool isTyping)
    {
        try
        {
            await _client.SendTypingIndicatorAsync(chatId, isTyping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send typing indicator for chat: {ChatId}", chatId);
            throw;
        }
    }

    public event Action<MessageDto>? MessageReceived;
    public event Action<MessageDto>? MessageUpdated;
    public event Action<Guid>? MessageDeleted;
    public event Action<Guid>? UserJoinedChat;
    public event Action<Guid>? UserLeftChat;
    public event Action<Guid, bool>? UserTyping;
    public event Action<Guid, bool>? UserOnlineStatusChanged;

    private void OnMessageReceived(MessageDto message)
    {
        MessageReceived?.Invoke(message);
    }

    private void OnMessageUpdated(MessageDto message)
    {
        MessageUpdated?.Invoke(message);
    }

    private void OnMessageDeleted(Guid messageId)
    {
        MessageDeleted?.Invoke(messageId);
    }

    private void OnUserJoinedChat(Guid chatId)
    {
        UserJoinedChat?.Invoke(chatId);
    }

    private void OnUserLeftChat(Guid chatId)
    {
        UserLeftChat?.Invoke(chatId);
    }

    private void OnUserTyping(Guid userId, bool isTyping)
    {
        UserTyping?.Invoke(userId, isTyping);
    }

    private void OnUserOnlineStatusChanged(Guid userId, bool isOnline)
    {
        UserOnlineStatusChanged?.Invoke(userId, isOnline);
    }
} 