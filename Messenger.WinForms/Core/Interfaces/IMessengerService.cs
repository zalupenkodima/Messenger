using Messenger.Shared;

namespace Messenger.WinForms.Core.Interfaces;

public interface IMessengerService
{
    // Authentication
    Task<bool> AuthenticateAsync(string username, string password);
    Task<bool> RegisterAsync(string username, string email, string password);
    void SetToken(string token);
    string? Token { get; }

    // Connection
    Task ConnectAsync();
    Task DisconnectAsync();
    bool IsConnected { get; }

    // Chats
    Task<IEnumerable<ChatDto>> GetChatsAsync();
    Task<ChatDto?> GetChatAsync(Guid chatId);
    Task<ChatDto> CreateChatAsync(CreateChatDto createChatDto);
    Task<ChatDto> UpdateChatAsync(Guid chatId, UpdateChatDto updateChatDto);
    Task AddMemberToChatAsync(Guid chatId, Guid memberId);
    Task RemoveMemberFromChatAsync(Guid chatId, Guid memberId);
    Task LeaveChatAsync(Guid chatId);
    Task DeleteChatAsync(Guid chatId);
    Task MarkChatAsReadAsync(Guid chatId);

    // Messages
    Task<IEnumerable<MessageDto>> GetChatMessagesAsync(Guid chatId, int skip = 0, int take = 50);
    Task<MessageDto?> GetMessageAsync(Guid messageId);
    Task<IEnumerable<MessageDto>> GetRepliesAsync(Guid messageId);
    Task<MessageDto> SendMessageAsync(CreateMessageDto createMessageDto);
    Task<MessageDto> UpdateMessageAsync(Guid messageId, UpdateMessageDto updateMessageDto);
    Task DeleteMessageAsync(Guid messageId);

    // Users
    Task<IEnumerable<UserDto>> SearchUsersAsync(string query);
    Task<UserDto?> GetUserAsync(Guid userId);
    Task<UserDto?> GetCurrentUserAsync();
    Task<IEnumerable<UserDto>> GetOnlineUsersAsync();

    // Real-time events
    event Action<MessageDto>? MessageReceived;
    event Action<MessageDto>? MessageUpdated;
    event Action<Guid>? MessageDeleted;
    event Action<Guid>? UserJoinedChat;
    event Action<Guid>? UserLeftChat;
    event Action<Guid, bool>? UserTyping;
    event Action<Guid, bool>? UserOnlineStatusChanged;

    // Typing indicators
    Task SendTypingIndicatorAsync(Guid chatId, bool isTyping);
} 