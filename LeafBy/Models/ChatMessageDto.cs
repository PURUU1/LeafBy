namespace LeafBy.Models
{
    public class ChatMessageDto
    {
        public string Role { get; set; } = string.Empty; // "user" or "model"
        public string Text { get; set; } = string.Empty;
    }
}