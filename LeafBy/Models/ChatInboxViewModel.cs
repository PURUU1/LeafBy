using System;

namespace LeafBy.Models
{
    public class ChatInboxViewModel
    {
        public string PartnerUsername { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
    }
}