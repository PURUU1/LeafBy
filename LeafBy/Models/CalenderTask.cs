namespace LeafBy.Models
{
    public class CalenderTask
    {
        public int Id { get; set; }
        public string AppUserId { get; set; } = string.Empty;
        
        public DateTime TaskDate { get; set; }
        public string TaskName { get; set; } = string.Empty; // e.g., "Sow Basil Seeds", "Fertilize Fern"
        public string Category { get; set; } = string.Empty; // e.g., "Sow", "Fertilize", "Water", "Prune"
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; } = false;
        // NEW: Nullable Foreign Key for the Plant
        public int? MyPlantId { get; set; }

        // Navigation property (Optional, but highly recommended)
        public Plant? MyPlant { get; set; }
    }
}
