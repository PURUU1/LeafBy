using Microsoft.AspNetCore.Identity;

namespace LeafBy.Models
{
    public class Appuser : IdentityUser
    {

        //public string? username { get; set; } = "Guest";
        // Navigation Properties (One User has Many of these)
        public ICollection<Plant>? MyPlants { get; set; } = new List<Plant>();
        public ICollection<CalenderTask>? MyTasks { get; set; } = new List<CalenderTask>();
        public ICollection<ComunityListing>? MyListings { get; set; } = new List<ComunityListing>();

        public String? lon { get; set; }
        public string? lat { get; set; }

        public string? country {get;set;}
        public string City { get; set; } = string.Empty;
        

    }
}
