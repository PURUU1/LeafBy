using System.Text.Json;
using LeafBy.Models;
using Microsoft.EntityFrameworkCore;

namespace LeafBy.Data
{
    public static class PlantCatalogSeeder
    {
        private const string ImageSubFolder = "images/plants";

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            // ── 1. Skip if already seeded ───────────────────────────────────
            if (await db.PlantCatalog.AnyAsync())
            {
                logger.LogInformation("PlantCatalogSeeder: already seeded, skipping.");
                return;
            }

            // ── 2. Read JSON directly into your models ──────────────────────
            var jsonPath = Path.Combine(
                AppContext.BaseDirectory, "Data", "SeedData", "plants.json");

            if (!File.Exists(jsonPath))
            {
                logger.LogWarning("PlantCatalogSeeder: plants.json not found at {Path}", jsonPath);
                return;
            }

            await using var stream = File.OpenRead(jsonPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Deserialize straight into your existing models — no DTOs needed
            var plants = await JsonSerializer.DeserializeAsync<List<PlantCatalog>>(stream, options)
                         ?? new List<PlantCatalog>();

            if (plants.Count == 0)
            {
                logger.LogWarning("PlantCatalogSeeder: no plants found in JSON.");
                return;
            }

            // ── 3. Ensure wwwroot/images/plants/ exists ─────────────────────
            var imageDir = Path.Combine(env.WebRootPath, ImageSubFolder);
            Directory.CreateDirectory(imageDir);

            // ── 4. Download images and swap remote URLs for local paths ──────
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("LeafBy-Seeder/1.0");
            http.Timeout = TimeSpan.FromSeconds(30);

            foreach (var plant in plants)
            {
                logger.LogInformation(
                    "PlantCatalogSeeder: processing [{Id}] {Name}", plant.Id, plant.CommonName);

                plant.OriginalUrl = await SaveImageAsync(plant.Id, "original", plant.OriginalUrl, imageDir, ImageSubFolder, http, logger);
                plant.ImageUrl = await SaveImageAsync(plant.Id, "image", plant.ImageUrl, imageDir, ImageSubFolder, http, logger);
                plant.RegularUrl = await SaveImageAsync(plant.Id, "regular", plant.RegularUrl, imageDir, ImageSubFolder, http, logger);
                plant.MediumUrl = await SaveImageAsync(plant.Id, "medium", plant.MediumUrl, imageDir, ImageSubFolder, http, logger);
                plant.SmallUrl = await SaveImageAsync(plant.Id, "small", plant.SmallUrl, imageDir, ImageSubFolder, http, logger);
                plant.Thumbnail = await SaveImageAsync(plant.Id, "thumb", plant.Thumbnail, imageDir, ImageSubFolder, http, logger);
            }

            // ── 5. Insert everything — clear tracked entities properly ──────────────
            // First, detach all tracked entities to prevent conflicts
            foreach (var plant in plants)
            {
                plant.Id = 0;  // Reset to 0 so EF knows to generate new ID

                if (plant.SoilMix != null)
                    plant.SoilMix.Id = 0;

                // --- ADD THIS MISSING BLOCK ---
                if (plant.CareProfile != null)
                    plant.CareProfile.Id = 0;
                // ------------------------------

                foreach (var remedy in plant.HomeRemedies)
                    remedy.Id = 0;
            }

            db.ChangeTracker.Clear();
            await db.PlantCatalog.AddRangeAsync(plants);
            await db.SaveChangesAsync();

            logger.LogInformation(
                "PlantCatalogSeeder: seeded {Count} plants successfully.", plants.Count);
        }

        // Downloads one image variant; returns the local web path on success,
        // or the original remote URL as fallback if the download fails.
        private static async Task<string?> SaveImageAsync(
            int plantId,
            string suffix,
            string? remoteUrl,
            string imageDir,
            string imageSubFolder,
            HttpClient http,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(remoteUrl))
                return null;

            var ext = GetExtension(remoteUrl);
            var fileName = $"{plantId}_{suffix}{ext}";
            var localPath = Path.Combine(imageDir, fileName);
            var webPath = $"/{imageSubFolder}/{fileName}";

            // Already downloaded on a previous run — skip
            if (File.Exists(localPath))
                return webPath;

            try
            {
                var bytes = await http.GetByteArrayAsync(remoteUrl);
                await File.WriteAllBytesAsync(localPath, bytes);
                logger.LogDebug("PlantCatalogSeeder: saved {File}", fileName);
                return webPath;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    "PlantCatalogSeeder: download failed for {Url} — {Msg}", remoteUrl, ex.Message);
                return remoteUrl; // fall back to remote so <img> still renders
            }
        }

        private static string GetExtension(string url)
        {
            try
            {
                var path = new Uri(url).AbsolutePath;
                var ext = Path.GetExtension(path);
                return string.IsNullOrEmpty(ext) ? ".jpg" : ext;
            }
            catch { return ".jpg"; }
        }
    }
}