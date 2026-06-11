using System.Text.Json.Serialization;

namespace LeafBy.ViewModels
{
    public class CalendarTaskViewModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } // Crucial for editing/completing tasks later!

        [JsonPropertyName("day")]
        public int Day { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("desc")]
        public string Description { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("theme")]
        public string Theme { get; set; }

        [JsonPropertyName("isCompleted")]
        public bool IsCompleted { get; set; }
    }
}
