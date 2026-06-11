    namespace LeafBy.Models
    {
        public class PlantCatalog
        {
            public int Id { get; set; }


            public string Nickname { get; set; } = string.Empty;
            public string CommonName { get; set; } = string.Empty;
            public string ScientificName { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty; // e.g., "Living Room", "Patio", "Terrace Garden"
            public string HealthStatus { get; set; } = string.Empty; // e.g., "Healthy", "Thirsty", "Thriving", "Needs Pruning"
            public string WaterRequirement { get; set; } = "Medium"; // "Low", "Medium", "High"
            public string SunRequirement { get; set; } = "Partial Sun"; // "Full Sun", "6+ Hours", "Indirect Light"

            // Navigation properties
            public SoilMix? SoilMix { get; set; }
            public ICollection<HomeRemdy> HomeRemedies { get; set; } = new List<HomeRemdy>();
            //images
            public string? OriginalUrl { get; set; } = string.Empty;
            public string? ImageUrl { get; set; } = string.Empty;
            public string? RegularUrl { get; set; } = string.Empty;
            public string? MediumUrl { get; set; } = string.Empty;
            public string? SmallUrl { get; set; } = string.Empty;
            public string? Thumbnail { get; set; } = string.Empty;

      
        // ADD THIS MISSING LINE:
        public PlantCareProfile? CareProfile { get; set; }

    }
    }
