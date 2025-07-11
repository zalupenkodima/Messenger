using Messenger.Domain.Entities;
using Messenger.Shared;

namespace Messenger.Domain.Interfaces;

public interface IChatRepository : IRepository<Chat>
{
    Task<IEnumerable<Chat>> GetUserChatsAsync(Guid userId);
    Task<Chat?> GetPrivateChatAsync(Guid user1Id, Guid user2Id);
    Task<IEnumerable<ChatMember>> GetChatMembersAsync(Guid chatId);
    Task<ChatMember?> GetChatMemberAsync(Guid chatId, Guid userId);
    Task AddMemberToChatAsync(Guid chatId, Guid userId, ChatRole role = ChatRole.Member);
    Task RemoveMemberFromChatAsync(Guid chatId, Guid userId);
    Task UpdateLastMessageAtAsync(Guid chatId, DateTime lastMessageAt);
    Task UpdateLastReadMessageAtAsync(Guid chatId, Guid userId, DateTime lastReadMessageAt);
} 