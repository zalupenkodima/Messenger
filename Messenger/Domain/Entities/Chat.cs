using System.ComponentModel.DataAnnotations;
using Messenger.Shared;

namespace Messenger.Domain.Entities;

public class Chat
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? AvatarUrl { get; set; }
    
    public ChatType Type { get; set; } = ChatType.Private;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<Message> Messages { get; set; } = [];
    public virtual ICollection<ChatMember> Members { get; set; } = [];
} 