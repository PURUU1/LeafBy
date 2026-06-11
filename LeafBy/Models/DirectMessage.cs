using System;
using System.ComponentModel.DataAnnotations;

namespace LeafBy.Models
{
    public class DirectMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderUsername { get; set; } = string.Empty;

        [Required]
        public string ReceiverUsername { get; set; } = string.Empty; // The specific person they are chatting with

        public string Message { get; set; } = string.Empty;

        public string MediaUrl { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}