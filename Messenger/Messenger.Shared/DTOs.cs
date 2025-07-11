namespace Messenger.Shared;

public enum ChatType
{
    Private,
    Group
}

public enum MessageType
{
    Text,
    Image,
    File,
    Voice,
    Video
}

public enum ChatRole
{
    Member,
    Admin,
    Owner
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsOnline { get; set; }
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
}

public class ChatDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public ChatType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MemberCount { get; set; }
    public int UnreadCount { get; set; }
    public List<ChatMemberDto> Members { get; set; } = new();
}

public class ChatMemberDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public ChatRole Role { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }
}

public class CreateChatDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChatType Type { get; set; } = ChatType.Private;
    public List<Guid> MemberIds { get; set; } = new();
}

public class UpdateChatDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public Guid ChatId { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public MessageDto? ReplyToMessage { get; set; }
    public int ReplyCount { get; set; }
}

public class CreateMessageDto
{
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public Guid ChatId { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}

public class UpdateMessageDto
{
    public string Content { get; set; } = string.Empty;
} 