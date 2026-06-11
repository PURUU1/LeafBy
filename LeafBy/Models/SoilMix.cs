using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeafBy.Models
{
    public class SoilMix
    {
        [Key]
        public int Id { get; set; }

        // 1. Explicitly link to PlantCatalog
        [ForeignKey("PlantCatalog")]
        public int? PlantCatalogId { get; set; }
        public PlantCatalog? PlantCatalog { get; set; }

        // 2. Explicitly link to the user's Plant
        [ForeignKey("Plant")]
        public int? PlantId { get; set; }
        public Plant? Plant { get; set; }

        public string RecipeName { get; set; } = string.Empty;
        public string Ingredients { get; set; } = string.Empty;

        public string Component1 { get; set; } = string.Empty;
        public int Percentage1 { get; set; }
        public string Component2 { get; set; } = string.Empty;
        public int Percentage2 { get; set; }
        public string Component3 { get; set; } = string.Empty;
        public int Percentage3 { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}