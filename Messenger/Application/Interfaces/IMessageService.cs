using Messenger.Shared;

namespace Messenger.Application.Interfaces;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(CreateMessageDto createMessageDto, Guid senderId);
    Task<MessageDto> UpdateMessageAsync(Guid messageId, UpdateMessageDto updateMessageDto, Guid userId);
    Task DeleteMessageAsync(Guid messageId, Guid userId);
    Task<IEnumerable<MessageDto>> GetChatMessagesAsync(Guid chatId, Guid userId, int skip = 0, int take = 50);
    Task<MessageDto?> GetMessageAsync(Guid messageId);
    Task<IEnumerable<MessageDto>> GetRepliesAsync(Guid messageId);
} 