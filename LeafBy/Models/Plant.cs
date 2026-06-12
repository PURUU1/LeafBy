using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeafBy.Models
{
    public class Plant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string AppUserId { get; set; } = string.Empty;

        public string Nickname { get; set; } = string.Empty;
        public string CommonName { get; set; } = string.Empty;
        public string ScientificName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty; // e.g., "Living Room", "Patio", "Terrace Garden"
        public string HealthStatus { get; set; } = string.Empty; // e.g., "Healthy", "Thirsty", "Thriving", "Needs Pruning"
        [Column(TypeName = "text")]
        public string ImageUrl { get; set; } = string.Empty;
        public string WaterRequirement { get; set; } = "Medium"; // "Low", "Medium", "High"
        public string SunRequirement { get; set; } = "Partial Sun"; // "Full Sun", "6+ Hours", "Indirect Light"

        // Inside Models/Plant.cs
        public PlantCareProfile? CareProfile { get; set; }

        // Navigation properties
        public SoilMix? SoilMix { get; set; }
        public ICollection<HomeRemdy> HomeRemedies { get; set; } = new List<HomeRemdy>();
    }
}
