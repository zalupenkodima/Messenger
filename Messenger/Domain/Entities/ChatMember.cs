using Messenger.Shared;

namespace Messenger.Domain.Entities;

public class ChatMember
{
    public Guid Id { get; set; }
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LeftAt { get; set; }
    
    public DateTime? LastReadMessageAt { get; set; }
    
    public ChatRole Role { get; set; } = ChatRole.Member;
    
    // Foreign keys
    public Guid UserId { get; set; }
    public Guid ChatId { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Chat Chat { get; set; } = null!;
} 