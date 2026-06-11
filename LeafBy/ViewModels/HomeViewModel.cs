using LeafBy.Models;
using System.ComponentModel.DataAnnotations;

namespace LeafBy.ViewModels
{
    public class HomeViewModel
    {
   
        [Required]
        public Appuser AppUser { get; set; } 

        public List<PlantCatalog>? PlantsCatalog { get; set; }

        public List<ListingRequest> IncomingRequests { get; set; } = new List<ListingRequest>();
        public List<ListingRequest> OutgoingRequests { get; set; } = new List<ListingRequest>();
        //dashboard items


    }
}
