using Microsoft.EntityFrameworkCore;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Messenger.Infrastructure.Data;
using Messenger.Shared;

namespace Messenger.Infrastructure.Repositories;

public class ChatRepository(MessengerDbContext context) : BaseRepository<Chat>(context), IChatRepository
{
    public async Task<IEnumerable<Chat>> GetUserChatsAsync(Guid userId)
    {
        return await _context.Chats
            .Include(c => c.Members)
            .Where(c => c.Members.Any(m => m.UserId == userId && m.LeftAt == null))
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();
    }

    public async Task<Chat?> GetPrivateChatAsync(Guid user1Id, Guid user2Id)
    {
        return await _context.Chats
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Type == ChatType.Private &&
                                     c.Members.Count == 2 &&
                                     c.Members.Any(m => m.UserId == user1Id && m.LeftAt == null) &&
                                     c.Members.Any(m => m.UserId == user2Id && m.LeftAt == null));
    }

    public async Task<IEnumerable<ChatMember>> GetChatMembersAsync(Guid chatId)
    {
        return await _context.ChatMembers
            .Include(cm => cm.User)
            .Where(cm => cm.ChatId == chatId && cm.LeftAt == null)
            .ToListAsync();
    }

    public async Task<ChatMember?> GetChatMemberAsync(Guid chatId, Guid userId)
    {
        return await _context.ChatMembers
            .Include(cm => cm.User)
            .FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == userId && cm.LeftAt == null);
    }

    public async Task AddMemberToChatAsync(Guid chatId, Guid userId, ChatRole role = ChatRole.Member)
    {
        var existingMember = await _context.ChatMembers
            .FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == userId);

        if (existingMember != null)
        {
            existingMember.LeftAt = null;
            existingMember.Role = role;
        }
        else
        {
            var chatMember = new ChatMember
            {
                ChatId = chatId,
                UserId = userId,
                Role = role
            };
            await _context.ChatMembers.AddAsync(chatMember);
        }

        await _context.SaveChangesAsync();
    }

    public async Task RemoveMemberFromChatAsync(Guid chatId, Guid userId)
    {
        var member = await _context.ChatMembers
            .FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == userId);

        if (member != null)
        {
            member.LeftAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateLastMessageAtAsync(Guid chatId, DateTime lastMessageAt)
    {
        var chat = await _dbSet.FindAsync(chatId);
        if (chat != null)
        {
            chat.LastMessageAt = lastMessageAt;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateLastReadMessageAtAsync(Guid chatId, Guid userId, DateTime lastReadMessageAt)
    {
        var member = await _context.ChatMembers
            .FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == userId && cm.LeftAt == null);

        if (member != null)
        {
            member.LastReadMessageAt = lastReadMessageAt;
            await _context.SaveChangesAsync();
        }
    }
} 