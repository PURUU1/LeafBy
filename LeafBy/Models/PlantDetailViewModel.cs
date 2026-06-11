using LeafBy.Models;

namespace LeafBy.ViewModels
{
    public class PlantDetailViewModel
    {
        // 1. The Main Plant Data
        public PlantCatalog Plant { get; set; }

        // 2. The Sidecar Care Details
        public PlantCareProfile CareProfile { get; set; }

        // 3. The Custom Soil Mix
        public SoilMix Soil { get; set; }

        // 4. The Separated Remedies
        public HomeRemdy CommercialFertilizer { get; set; }
        public HomeRemdy OrganicRemedy { get; set; }

        // Constructor to ensure we never pass null objects to the View, 
        // which prevents "Object reference not set to an instance" errors.
        public PlantDetailViewModel()
        {
            Plant = new PlantCatalog();
            CareProfile = new PlantCareProfile();
            Soil = new SoilMix();

            // Defaulting the types just in case they are empty in the DB
            CommercialFertilizer = new HomeRemdy { Type = "Commercial" };
            OrganicRemedy = new HomeRemdy { Type = "Organic" };
        }
    }
}