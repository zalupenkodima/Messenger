using Microsoft.EntityFrameworkCore;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Messenger.Infrastructure.Data;

namespace Messenger.Infrastructure.Repositories;

public class MessageRepository(MessengerDbContext context) : BaseRepository<Message>(context), IMessageRepository
{
    public async Task<IEnumerable<Message>> GetChatMessagesAsync(Guid chatId, int skip = 0, int take = 50)
    {
        return await _dbSet
            .Include(m => m.Sender)
            .Include(m => m.ReplyToMessage)
            .Where(m => m.ChatId == chatId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Message?> GetMessageWithRepliesAsync(Guid messageId)
    {
        return await _dbSet
            .Include(m => m.Sender)
            .Include(m => m.ReplyToMessage)
            .Include(m => m.Replies)
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);
    }

    public async Task<IEnumerable<Message>> GetRepliesAsync(Guid messageId)
    {
        return await _dbSet
            .Include(m => m.Sender)
            .Where(m => m.ReplyToMessageId == messageId && !m.IsDeleted)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task SoftDeleteMessageAsync(Guid messageId)
    {
        var message = await _dbSet.FindAsync(messageId);
        if (message != null)
        {
            message.IsDeleted = true;
            message.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateMessageContentAsync(Guid messageId, string newContent)
    {
        var message = await _dbSet.FindAsync(messageId);
        if (message != null)
        {
            message.Content = newContent;
            message.EditedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid chatId, Guid userId)
    {
        var chatMember = await _context.ChatMembers
            .FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == userId && cm.LeftAt == null);

        if (chatMember?.LastReadMessageAt == null)
        {
            return await _dbSet
                .CountAsync(m => m.ChatId == chatId && 
                                m.SenderId != userId && 
                                !m.IsDeleted &&
                                m.CreatedAt > DateTime.UtcNow.AddDays(-7));
        }

        return await _dbSet
            .CountAsync(m => m.ChatId == chatId && 
                            m.SenderId != userId && 
                            !m.IsDeleted &&
                            m.CreatedAt > chatMember.LastReadMessageAt.Value);
    }

    public async Task<IEnumerable<Message>> GetMessagesOlderThanAsync(DateTime threshold)
    {
        return await _dbSet
            .Include(m => m.Sender)
            .Include(m => m.Chat)
            .Where(m => m.CreatedAt < threshold && !m.IsDeleted)
            .ToListAsync();
    }
} 