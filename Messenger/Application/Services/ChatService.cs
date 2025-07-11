using AutoMapper;
using Messenger.Application.Interfaces;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Messenger.Shared;

namespace Messenger.Application.Services;

public class ChatService(
    IChatRepository chatRepository,
    IUserRepository userRepository,
    IMessageRepository messageRepository,
    IMapper mapper,
    ILogger<ChatService> logger) : IChatService
{
    private readonly IChatRepository _chatRepository = chatRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IMessageRepository _messageRepository = messageRepository;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<ChatService> _logger = logger;

    public async Task<ChatDto> CreateChatAsync(CreateChatDto createChatDto, Guid creatorId)
    {
        _logger.LogInformation("Creating chat for creator {CreatorId}", creatorId);
        
        var creator = await _userRepository.GetByIdAsync(creatorId);
        if (creator == null)
            throw new InvalidOperationException("Creator not found");

        if (createChatDto.Type == ChatType.Private && createChatDto.MemberIds.Count == 2)
        {
            var existingChat = await _chatRepository.GetPrivateChatAsync(
                createChatDto.MemberIds[0], createChatDto.MemberIds[1]);
            if (existingChat != null)
            {
                _logger.LogInformation("Existing private chat found: {ChatId}", existingChat.Id);
                return await MapToDtoAsync(existingChat, creatorId);
            }
        }

        var chat = _mapper.Map<Chat>(createChatDto);
        var savedChat = await _chatRepository.AddAsync(chat);
        _logger.LogInformation("Chat created: {ChatId}", savedChat.Id);

        await _chatRepository.AddMemberToChatAsync(savedChat.Id, creatorId, ChatRole.Owner);
        _logger.LogInformation("Creator {CreatorId} added as owner to chat {ChatId}", creatorId, savedChat.Id);

        foreach (var memberId in createChatDto.MemberIds.Where(id => id != creatorId))
        {
            var member = await _userRepository.GetByIdAsync(memberId);
            if (member != null)
            {
                await _chatRepository.AddMemberToChatAsync(savedChat.Id, memberId, ChatRole.Member);
                _logger.LogInformation("Member {MemberId} added to chat {ChatId}", memberId, savedChat.Id);
            }
        }

        return await MapToDtoAsync(savedChat, creatorId);
    }

    public async Task<ChatDto> UpdateChatAsync(Guid chatId, UpdateChatDto updateChatDto, Guid userId)
    {
        var chatMember = await _chatRepository.GetChatMemberAsync(chatId, userId);
        if (chatMember == null)
            throw new InvalidOperationException("User is not a member of this chat");

        if (chatMember.Role != ChatRole.Owner && chatMember.Role != ChatRole.Admin)
            throw new UnauthorizedAccessException("Only owners and admins can update chat");

        var chat = await _chatRepository.GetByIdAsync(chatId);
        if (chat == null)
            throw new InvalidOperationException("Chat not found");

        if (!string.IsNullOrEmpty(updateChatDto.Name))
            chat.Name = updateChatDto.Name;

        if (!string.IsNullOrEmpty(updateChatDto.Description))
            chat.Description = updateChatDto.Description;

        if (!string.IsNullOrEmpty(updateChatDto.AvatarUrl))
            chat.AvatarUrl = updateChatDto.AvatarUrl;

        var updatedChat = await _chatRepository.UpdateAsync(chat);
        return await MapToDtoAsync(updatedChat, userId);
    }

    public async Task<IEnumerable<ChatDto>> GetUserChatsAsync(Guid userId)
    {
        _logger.LogInformation("Getting chats for user {UserId}", userId);
        var chats = await _chatRepository.GetUserChatsAsync(userId);
        _logger.LogInformation("Found {ChatCount} chats for user {UserId}", chats.Count(), userId);
        
        var chatDtos = new List<ChatDto>();

        foreach (var chat in chats)
            chatDtos.Add(await MapToDtoAsync(chat, userId));

        return chatDtos;
    }

    public async Task<ChatDto?> GetChatAsync(Guid chatId, Guid userId)
    {
        var chatMember = await _chatRepository.GetChatMemberAsync(chatId, userId);
        if (chatMember == null)
            return null;

        var chat = await _chatRepository.GetByIdAsync(chatId);
        return chat != null ? await MapToDtoAsync(chat, userId) : null;
    }

    public async Task AddMemberToChatAsync(Guid chatId, Guid memberId, Guid userId)
    {
        var chatMember = await _chatRepository.GetChatMemberAsync(chatId, userId);
        if (chatMember == null)
            throw new InvalidOperationException("User is not a member of this chat");

        if (chatMember.Role != ChatRole.Owner && chatMember.Role != ChatRole.Admin)
            throw new UnauthorizedAccessException("Only owners and admins can add members");

        var member = await _userRepository.GetByIdAsync(memberId);
        if (member == null)
            throw new InvalidOperationException("User to add not found");

        await _chatRepository.AddMemberToChatAsync(chatId, memberId, ChatRole.Member);
    }

    public async Task RemoveMemberFromChatAsync(Guid chatId, Guid memberId, Guid userId)
    {
        var chatMember = await _chatRepository.GetChatMemberAsync(chatId, userId);
        if (chatMember == null)
            throw new InvalidOperationException("User is not a member of this chat");

        if (chatMember.Role != ChatRole.Owner && chatMember.Role != ChatRole.Admin)
            throw new UnauthorizedAccessException("Only owners and admins can remove members");

        var memberToRemove = await _chatRepository.GetChatMemberAsync(chatId, memberId);
        if (memberToRemove == null)
            throw new InvalidOperationException("Member not found in chat");

        if (memberToRemove.Role == ChatRole.Owner)
            throw new UnauthorizedAccessException("Cannot remove chat owner");

        await _chatRepository.RemoveMemberFromChatAsync(chatId, memberId);
    }

    public async Task LeaveChatAsync(Guid chatId, Guid userId)
    {
        var chatMember = await _chatRepository.GetChatMemberAsync(chatId, userId);
        if (chatMember == null)
            throw new InvalidOperationException("User is not a member of this chat");

        if (chatMember.Role == ChatRole.Owner)
            throw new InvalidOperationException("Chat owner cannot leave the chat");

        await _chatRepository.RemoveMemberFromChatAsync(chatId, userId);
    }

    public async Task DeleteChatAsync(Guid chatId, Guid userId)
    {
        var chatMember = await _chatRepository.GetChatMemberAsync(chatId, userId);
        if (chatMember == null)
            throw new InvalidOperationException("User is not a member of this chat");

        if (chatMember.Role != ChatRole.Owner)
            throw new UnauthorizedAccessException("Only chat owner can delete chat");

        await _chatRepository.DeleteAsync(chatId);
    }

    public async Task MarkChatAsReadAsync(Guid chatId, Guid userId)
    {
        _logger.LogInformation("Marking chat {ChatId} as read for user {UserId}", chatId, userId);
        
        var chatMember = await _chatRepository.GetChatMemberAsync(chatId, userId);
        if (chatMember == null)
            throw new InvalidOperationException("User is not a member of this chat");

        await _chatRepository.UpdateLastReadMessageAtAsync(chatId, userId, DateTime.UtcNow);
        
        _logger.LogInformation("Chat {ChatId} marked as read for user {UserId}", chatId, userId);
    }

    private async Task<ChatDto> MapToDtoAsync(Chat chat, Guid userId)
    {
        var dto = _mapper.Map<ChatDto>(chat);
        dto.UnreadCount = await _messageRepository.GetUnreadCountAsync(chat.Id, userId);
        
        var members = await _chatRepository.GetChatMembersAsync(chat.Id);
        dto.Members = new List<ChatMemberDto>();
        
        foreach (var member in members)
        {
            var user = await _userRepository.GetByIdAsync(member.UserId);
            if (user != null)
            {
                dto.Members.Add(new ChatMemberDto
                {
                    UserId = user.Id,
                    Username = user.Username,
                    AvatarUrl = user.AvatarUrl,
                    Role = member.Role,
                    IsOnline = user.IsOnline,
                    LastSeen = user.LastSeen
                });
            }
        }
        
        if (chat.Type == ChatType.Private)
        {
            var otherMember = members.FirstOrDefault(m => m.UserId != userId);
            if (otherMember != null)
            {
                var otherUser = await _userRepository.GetByIdAsync(otherMember.UserId);
                if (otherUser != null)
                {
                    dto.Name = otherUser.Username;
                    dto.AvatarUrl = otherUser.AvatarUrl;
                }
            }
        }
        
        return dto;
    }
} 