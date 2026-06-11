using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeafBy.Models
{
    // 1. The Sidecar table for the extra UI text
    public class PlantCareProfile
    {
        [Key]
        public int Id { get; set; }

        // Foreign Key pointing to the locked PlantCatalog
        [ForeignKey("PlantCatalog")]
        public int? PlantCatalogId { get; set; }
        public PlantCatalog? PlantCatalog { get; set; }

        // 2. Explicitly link to the user's Plant
        [ForeignKey("Plant")]
        public int? PlantId { get; set; }
        public Plant? Plant { get; set; }

        // The extra details for the top 3 widgets
        public string SunlightDetails { get; set; } = string.Empty; // e.g., "Full direct exposure"
        public string WaterDetails { get; set; } = string.Empty;    // e.g., "Let soil dry fully"
        public string PruningRequirement { get; set; } = string.Empty; // e.g., "Spring"
        public string PruningDetails { get; set; } = string.Empty;     // e.g., "Trim after flowering"

        // Bottom green card
        public string ThermalSensitivity { get; set; } = string.Empty;
    }

    


}