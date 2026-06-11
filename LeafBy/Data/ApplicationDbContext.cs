using LeafBy.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LeafBy.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }


        public DbSet<LeafBy.Models.DirectMessage> DirectMessages { get; set; }
        public DbSet<PlantCareProfile> PlantCareProfiles { get; set; }
        //public DbSet<SoilMix> SoilMixes { get; set; }
        public DbSet<ListingRequest> ListingRequests { get; set; }
        public DbSet<Plant> Plants => Set<Plant>();
        public DbSet<PlantCatalog> PlantCatalog => Set<PlantCatalog>();
        public DbSet<SoilMix> SoilMixes => Set<SoilMix>();
        public DbSet<HomeRemdy> HomeRemedies => Set<HomeRemdy>();
        public DbSet<CalenderTask> CalendarTasks => Set<CalenderTask>();
        public DbSet<ComunityListing> CommunityListings => Set<ComunityListing>();
        public DbSet<Appuser> AppUsers=> Set<Appuser>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // If you are using ASP.NET Identity, you MUST keep this base call:
            base.OnModelCreating(modelBuilder);

            // 1. FORCE HOME REMEDY RELATIONSHIPS
            modelBuilder.Entity<HomeRemdy>()
                .HasOne(h => h.PlantCatalog)
                .WithMany(c => c.HomeRemedies) // Assuming PlantCatalog has ICollection<HomeRemdy>
                .HasForeignKey(h => h.PlantCatalogId)
                .OnDelete(DeleteBehavior.Cascade); // Catalog deletion cleans up its remedies

            modelBuilder.Entity<HomeRemdy>()
                .HasOne(h => h.Plant)
                .WithMany(p => p.HomeRemedies) // Assuming Plant has ICollection<HomeRemdy>
                .HasForeignKey(h => h.PlantId)
                .OnDelete(DeleteBehavior.Cascade); // User plant deletion cleans up its remedies

            // 2. FORCE SOIL MIX RELATIONSHIPS (Assuming 1-to-1 relationship)
            modelBuilder.Entity<SoilMix>()
                .HasOne(s => s.PlantCatalog)
                .WithOne(c => c.SoilMix)
                .HasForeignKey<SoilMix>(s => s.PlantCatalogId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SoilMix>()
                .HasOne(s => s.Plant)
                .WithOne(p => p.SoilMix)
                .HasForeignKey<SoilMix>(s => s.PlantId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. FORCE CARE PROFILE RELATIONSHIPS (Assuming 1-to-1 relationship)
            modelBuilder.Entity<PlantCareProfile>()
                .HasOne(c => c.PlantCatalog)
                .WithOne(p => p.CareProfile)
                .HasForeignKey<PlantCareProfile>(c => c.PlantCatalogId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlantCareProfile>()
                .HasOne(c => c.Plant)
                .WithOne(p => p.CareProfile)
                .HasForeignKey<PlantCareProfile>(c => c.PlantId)
                .OnDelete(DeleteBehavior.Cascade);


            // ... existing Fluent API rules for SoilMix, etc ...

            // FIX FOR LISTING REQUEST CASCADE PATH ERROR
            modelBuilder.Entity<ListingRequest>()
                .HasOne(lr => lr.CommunityListing)
                .WithMany(c => c.ListingRequests) // Leave blank inside the parenthesis if CommunityListing doesn't have a List<ListingRequest>
                .HasForeignKey(lr => lr.ComunityListingId) // NOTE: Using the exact spelling from your error message
                .OnDelete(DeleteBehavior.Restrict); // <--- THIS IS THE MAGIC FIX!
        }

    }
    //public static class DbInitializer
    //{
    //    public static void Initialize(ApplicationDbContext context)
    //    {
    //        // Ensure the database is created
    //        context.Database.EnsureCreated();

    //        // Check if seeding is already done
    //        if (context.Plants.Any())
    //        {
    //            return; // Database has been seeded
    //        }

    //        // --- 1. SEED PLANTS ---
    //        var plants = new List<Plant>
    //        {
    //            new Plant
    //            {
    //                Nickname = "Monstera Deliciosa",
    //                CommonName = "Swiss Cheese Plant",
    //                ScientificName = "Monstera Deliciosa",
    //                Location = "Living Room",
    //                HealthStatus = "Healthy",
    //                WaterRequirement = "Medium",
    //                SunRequirement = "Indirect Light",
    //                ImageUrl = "https://images.unsplash.com/photo-1614594975525-e45190c55d0b?auto=format&fit=crop&q=80&w=400"
    //            },
    //            new Plant
    //            {
    //                Nickname = "Succulent Bowl",
    //                CommonName = "Mixed Echeveria & Sedum",
    //                ScientificName = "Succulentae Bowl",
    //                Location = "Patio",
    //                HealthStatus = "Thirsty",
    //                WaterRequirement = "Low",
    //                SunRequirement = "Full Sun",
    //                ImageUrl = "https://images.unsplash.com/photo-1509440159596-0249088772ff?auto=format&fit=crop&q=80&w=400"
    //            },
    //            new Plant
    //            {
    //                Nickname = "Fiddle Leaf Fig",
    //                CommonName = "Fiddle-Leaf Fig",
    //                ScientificName = "Ficus Lyrata",
    //                Location = "Bedroom",
    //                HealthStatus = "Thriving",
    //                WaterRequirement = "Medium",
    //                SunRequirement = "6+ Hours",
    //                ImageUrl = "https://images.unsplash.com/photo-1597055181300-e3633a207518?auto=format&fit=crop&q=80&w=400"
    //            }
    //        };

    //        context.Plants.AddRange(plants);
    //        context.SaveChanges(); // Persist plants to generate IDs for relationships

    //        // --- 2. SEED SOIL MIXES & HOME REMEDIES (Referencing Seeded Plants) ---
    //        var monstera = plants[0];
    //        var succulent = plants[1];
    //        var fig = plants[2];

        

    //        // --- 3. SEED CALENDAR TASKS ---
    //        // Creating relative dates so the seeded tasks always land on active days surrounding the run date.
    //        var today = DateTime.Today;
    //        var calendarTasks = new List<CalenderTask>
    //        {
    //            new CalenderTask
    //            {
    //                TaskDate = today.AddDays(1 - (int)today.DayOfWeek + 2), // Wednesday of current week
    //                TaskName = "Fertilize Fern",
    //                Category = "Fertilize",
    //                Description = "Apply generic diluted organic leaf booster to guarantee deep foliage color.",
    //                IsCompleted = false
    //            },
    //            new CalenderTask
    //            {
    //                TaskDate = today.AddDays(1 - (int)today.DayOfWeek + 4), // Friday of current week
    //                TaskName = "Sow Basil Seeds",
    //                Category = "Sow",
    //                Description = "Sprinkle basil seeds in damp high-nitrogen starter mix under a warmth dome.",
    //                IsCompleted = false
    //            }
    //        };

    //        context.CalendarTasks.AddRange(calendarTasks);

    //        // --- 4. SEED COMMUNITY LISTINGS ---
    //        var listings = new List<ComunityListing>
    //        {
    //            new ComunityListing
    //            {
    //                Title = "Pothos Cutting",
    //                Category = "Giveaway",
    //                PlantType = "Cutting",
    //                DistanceMiles = 0.4,
    //                OwnerName = "Sarah G.",
    //                ImageUrl = "https://images.unsplash.com/photo-1545241047-6083a3684587?auto=format&fit=crop&q=80&w=400",
    //                Description = "Healthy, rooted golden pothos cutting ready to go into soil or water jar setup."
    //            },
    //            new ComunityListing
    //            {
    //                Title = "Lavender Pot",
    //                Category = "Wanted",
    //                PlantType = "Sapling",
    //                DistanceMiles = 1.2,
    //                OwnerName = "Mark T.",
    //                ImageUrl = "https://images.unsplash.com/photo-1528183429752-a97d0bf99b5a?auto=format&fit=crop&q=80&w=400",
    //                Description = "Looking for a mature English Lavender seedling to add to my sunny deck layout."
    //            },
    //            new ComunityListing
    //            {
    //                Title = "Jade Bonsai",
    //                Category = "Giveaway",
    //                PlantType = "Sapling",
    //                DistanceMiles = 2.5,
    //                OwnerName = "Lia K.",
    //                ImageUrl = "https://images.unsplash.com/photo-1512428559087-560fa5ceab42?auto=format&fit=crop&q=80&w=400",
    //                Description = "Small miniature Jade tree propagated from my main mother succulent container."
    //            }
    //        };

    //        context.CommunityListings.AddRange(listings);

    //        // Commit all seeded records
    //        context.SaveChanges();
    //    }
    //}
}
