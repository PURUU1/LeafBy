using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeafBy.Models
{
    public class ComunityListing
    {
        public int Id { get; set; }

        public string AppUserId { get; set; } = string.Empty;
        public Appuser? User { get; set; }
        public string Title { get; set; } = string.Empty; // e.g., "Pothos Cutting"
        public string Category { get; set; } = string.Empty; // "Giveaway" or "Wanted"
        public string PlantType { get; set; } = string.Empty; // "Cutting", "Sapling", "Seeds"
        public double DistanceMiles { get; set; }
        public string OwnerName { get; set; } = string.Empty; // e.g., "Sarah G."
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        [Required]
        public string ListingType { get; set; } = string.Empty; // e.g., "Trade", "Sell", "Free", "Wanted"

        public string? Latitude { get; set; }
        public string? Longitude { get; set; }

        public ICollection<ListingRequest> ListingRequests { get; set; } = new List<ListingRequest>();

    }
}
