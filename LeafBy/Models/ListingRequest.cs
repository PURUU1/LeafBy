using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeafBy.Models
{
    public class ListingRequest
    {
        [Key]
        public int Id { get; set; }

        // The specific item being requested
        //public int ComunityListingId { get; set; }
        public int ComunityListingId { get; set; }

        // 2. ADD THIS MISSING LINE: The Navigation Property
        [ForeignKey("ComunityListingId")]
        public ComunityListing? CommunityListing { get; set; }
        public ComunityListing? Listing { get; set; }

        // The user who made the request
        public string RequesterId { get; set; } = string.Empty;
        public Appuser? Requester { get; set; }

        // The user who owns the listing (Stored for easier queries)
        public string OwnerId { get; set; } = string.Empty;

        // The message sent with the request (e.g., "I have a spider plant to trade!")
        public string Message { get; set; } = string.Empty;

        // Status tracking: "Pending", "Accepted", "Rejected"
        public string Status { get; set; } = "Pending";

        public DateTime DateRequested { get; set; } = DateTime.UtcNow;
    }
}