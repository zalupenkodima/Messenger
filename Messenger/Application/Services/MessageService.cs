using AutoMapper;
using Messenger.Application.Interfaces;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Messenger.Shared;

namespace Messenger.Application.Services;

public class MessageService(
    IMessageRepository messageRepository,
    IChatRepository chatRepository,
    IMapper mapper,
    ILogger<MessageService> logger) : IMessageService
{
    private readonly IMessageRepository _messageRepository = messageRepository;
    private readonly IChatRepository _chatRepository = chatRepository;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<MessageService> _logger = logger;

    public async Task<MessageDto> SendMessageAsync(CreateMessageDto createMessageDto, Guid senderId)
    {
        _logger.LogInformation("User {SenderId} trying to send message to chat {ChatId}", senderId, createMessageDto.ChatId);
        
        var chatMember = await _chatRepository.GetChatMemberAsync(createMessageDto.ChatId, senderId);
        if (chatMember == null)
        {
            _logger.LogError("User {SenderId} is not a member of chat {ChatId}", senderId, createMessageDto.ChatId);
            throw new InvalidOperationException("User is not a member of this chat");
        }

        _logger.LogInformation("User {SenderId} is confirmed as member of chat {ChatId}", senderId, createMessageDto.ChatId);

        var message = new Message
        {
            Content = createMessageDto.Content,
            Type = createMessageDto.Type,
            SenderId = senderId,
            ChatId = createMessageDto.ChatId,
            ReplyToMessageId = createMessageDto.ReplyToMessageId
        };

        var savedMessage = await _messageRepository.AddAsync(message);
        _logger.LogInformation("Message saved with ID: {MessageId}", savedMessage.Id);
        
        await _chatRepository.UpdateLastMessageAtAsync(createMessageDto.ChatId, DateTime.UtcNow);

        return await MapToDtoAsync(savedMessage);
    }

    public async Task<MessageDto> UpdateMessageAsync(Guid messageId, UpdateMessageDto updateMessageDto, Guid userId)
    {
        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message == null)
            throw new InvalidOperationException("Message not found");

        if (message.SenderId != userId)
            throw new UnauthorizedAccessException("You can only edit your own messages");

        await _messageRepository.UpdateMessageContentAsync(messageId, updateMessageDto.Content);
        
        var updatedMessage = await _messageRepository.GetByIdAsync(messageId);
        return await MapToDtoAsync(updatedMessage!);
    }

    public async Task DeleteMessageAsync(Guid messageId, Guid userId)
    {
        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message == null)
            throw new InvalidOperationException("Message not found");

        if (message.SenderId != userId)
            throw new UnauthorizedAccessException("You can only delete your own messages");

        await _messageRepository.SoftDeleteMessageAsync(messageId);
    }

    public async Task<IEnumerable<MessageDto>> GetChatMessagesAsync(Guid chatId, Guid userId, int skip = 0, int take = 50)
    {
        var chatMember = await _chatRepository.GetChatMemberAsync(chatId, userId);
        if (chatMember == null)
            throw new InvalidOperationException("User is not a member of this chat");

        var messages = await _messageRepository.GetChatMessagesAsync(chatId, skip, take);
        var messageDtos = new List<MessageDto>();

        foreach (var message in messages)
            messageDtos.Add(await MapToDtoAsync(message));

        return messageDtos;
    }

    public async Task<MessageDto?> GetMessageAsync(Guid messageId)
    {
        var message = await _messageRepository.GetMessageWithRepliesAsync(messageId);
        return message != null ? await MapToDtoAsync(message) : null;
    }

    public async Task<IEnumerable<MessageDto>> GetRepliesAsync(Guid messageId)
    {
        var replies = await _messageRepository.GetRepliesAsync(messageId);
        var replyDtos = new List<MessageDto>();

        foreach (var reply in replies)
            replyDtos.Add(await MapToDtoAsync(reply));

        return replyDtos;
    }

    private async Task<MessageDto> MapToDtoAsync(Message message)
    {
        var dto = _mapper.Map<MessageDto>(message);

        var replies = await _messageRepository.GetRepliesAsync(message.Id);
        dto.ReplyCount = replies.Count();

        return dto;
    }
} 