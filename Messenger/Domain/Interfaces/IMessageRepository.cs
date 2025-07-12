using Messenger.Domain.Entities;

namespace Messenger.Domain.Interfaces;

public interface IMessageRepository : IRepository<Message>
{
    Task<IEnumerable<Message>> GetChatMessagesAsync(Guid chatId, int skip = 0, int take = 50);
    Task<Message?> GetMessageWithRepliesAsync(Guid messageId);
    Task<IEnumerable<Message>> GetRepliesAsync(Guid messageId);
    Task SoftDeleteMessageAsync(Guid messageId);
    Task UpdateMessageContentAsync(Guid messageId, string newContent);
    Task<int> GetUnreadCountAsync(Guid chatId, Guid userId);
    Task<IEnumerable<Message>> GetMessagesOlderThanAsync(DateTime threshold);
} 