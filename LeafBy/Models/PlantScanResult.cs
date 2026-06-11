namespace LeafBy.Models
{
    public class PlantScanResult
    {
        // Tells the view which UI to render!
        public string ScanCategory { get; set; } = string.Empty;

        // ==========================================
        // 1. PLANT ID PROPERTIES
        // ==========================================
        public string PlantName { get; set; }
        public string ScientificName { get; set; }
        public CareStat Sunlight { get; set; }
        public CareStat Watering { get; set; }
        public CareStat Pruning { get; set; }
        public List<SoilComponent> SoilMix { get; set; } = new List<SoilComponent>();
        public string SoilNote { get; set; }
        public Nutrition Nutrition { get; set; }
        public string ThermalSensitivity { get; set; }

        // ==========================================
        // 2. PEST ID PROPERTIES
        // ==========================================
        public string PestName { get; set; }
        public string ThreatLevel { get; set; }
        public string AffectedAreas { get; set; }
        public PestTreatment Treatment { get; set; } // We will create this class below
        public string PreventionTip { get; set; }

        // ==========================================
        // 3. DISEASE ID PROPERTIES
        // ==========================================
        public string Diagnosis { get; set; }
        public string Severity { get; set; }
        public string Symptoms { get; set; }
        public List<string> ActionPlan { get; set; } = new List<string>();
        public HomeRemedyResult? HomeRemedy { get; set; }

        // Add this helper class at the bottom of the file
    }
        public class HomeRemedyResult
        {
            public string? Type { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? Instructions { get; set; }
        }

    // --- Supporting Classes ---
    public class CareStat { public string Amount { get; set; } public string Detail { get; set; } }
    public class SoilComponent { public string Name { get; set; } public int Percentage { get; set; } }
    public class Nutrition { public Fertilizer Commercial { get; set; } public Fertilizer Organic { get; set; } }
    public class Fertilizer { public string Name { get; set; } public string Benefits { get; set; } public string Schedule { get; set; } public string Recipe { get; set; } }

    // NEW: Supporting class specifically for the Pest JSON
    public class PestTreatment
    {
        public string Commercial { get; set; }
        public string Organic { get; set; }

    }
}