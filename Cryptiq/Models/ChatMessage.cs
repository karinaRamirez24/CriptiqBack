namespace CryptiqChat.Models
{
    public class ChatMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SenderId { get; set; }
        public Guid? ReceiverId { get; set; }   // Null si es grupal
        public Guid? GroupId { get; set; }       // Null si es 1-a-1
        public string EncryptedPayload { get; set; } = string.Empty;
        public string QrData { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int StatusId { get; set; }
        public MessageStatus Status { get; set; }
        // Navegación
        public User? Sender { get; set; }
        public User? Receiver { get; set; }
    }
}