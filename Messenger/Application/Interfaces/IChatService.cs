using Messenger.Shared;

namespace Messenger.Application.Interfaces;

public interface IChatService
{
    Task<ChatDto> CreateChatAsync(CreateChatDto createChatDto, Guid creatorId);
    Task<ChatDto> UpdateChatAsync(Guid chatId, UpdateChatDto updateChatDto, Guid userId);
    Task<IEnumerable<ChatDto>> GetUserChatsAsync(Guid userId);
    Task<ChatDto?> GetChatAsync(Guid chatId, Guid userId);
    Task AddMemberToChatAsync(Guid chatId, Guid memberId, Guid userId);
    Task RemoveMemberFromChatAsync(Guid chatId, Guid memberId, Guid userId);
    Task LeaveChatAsync(Guid chatId, Guid userId);
    Task DeleteChatAsync(Guid chatId, Guid userId);
    Task MarkChatAsReadAsync(Guid chatId, Guid userId);
} 