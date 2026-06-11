using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeafBy.Models
{
    public class HomeRemdy
    {
        [Key]
        public int Id { get; set; }

        // 1. The Column
        public int? PlantCatalogId { get; set; }
        // 2. The Link
        [ForeignKey("PlantCatalogId")]
        public PlantCatalog? PlantCatalog { get; set; }

        // 1. The Column
        public int? PlantId { get; set; }
        // 2. The Link
        [ForeignKey("PlantId")]
        public Plant? Plant { get; set; }

        public string Type { get; set; } = "Organic";
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
    }
}