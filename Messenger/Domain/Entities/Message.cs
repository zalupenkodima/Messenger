using System.ComponentModel.DataAnnotations;
using Messenger.Shared;

namespace Messenger.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public MessageType Type { get; set; } = MessageType.Text;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? EditedAt { get; set; }
    
    public bool IsDeleted { get; set; }
    
    public DateTime? DeletedAt { get; set; }
    
    public Guid SenderId { get; set; }
    public Guid ChatId { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    
    public virtual User Sender { get; set; } = null!;
    public virtual Chat Chat { get; set; } = null!;
    public virtual Message? ReplyToMessage { get; set; }
    public virtual ICollection<Message> Replies { get; set; } = [];
} 