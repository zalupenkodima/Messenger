using System.ComponentModel.DataAnnotations;

namespace Messenger.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public string? AvatarUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    
    public bool IsOnline { get; set; }
    
    public virtual ICollection<Message> Messages { get; set; } = [];
    public virtual ICollection<Chat> Chats { get; set; } = [];
    public virtual ICollection<ChatMember> ChatMemberships { get; set; } = [];
} 