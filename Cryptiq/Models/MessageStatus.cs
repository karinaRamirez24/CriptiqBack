using CryptiqChat.Models;

public class MessageStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

