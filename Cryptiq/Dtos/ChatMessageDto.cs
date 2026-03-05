namespace CryptiqChat.Dtos
{
    public class ChatMessageDto
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public Guid? ReceiverId { get; set; }
        public Guid? GroupId { get; set; }
        public string Payload { get; set; }
        public string QrData { get; set; }
        public string CreatedAt { get; set; } // formato legible
        public int StatusId { get; set; }
    }
}
