//using LeafBy.Models;
//using System.Text.Json;

//namespace LeafBy.Data
//{
//    public class PerenualApiService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly IConfiguration _Configration;
//        //private readonly string _apiKey = "YOUR_PERENUAL_API_KEY"; // Store in appsettings.json in production

//        public PerenualApiService(HttpClient httpClient, IConfiguration Config)
//        {
//            _httpClient = httpClient;
//            _Configration = Config;
//        }


//        // NEW METHOD: Fetches a sequential batch of plants (e.g., IDs 1 through 60)
//        public async Task<List<PlantCatalog>> FetchPlantsSequentiallyAsync(int startId = 1, int endId = 60)
//        {
//            var plantsList = new List<PlantCatalog>();

//            for (int i = startId; i <= endId; i++)
//            {
//                var plant = await FetchPlantByIdAsync(i);

//                if (plant != null && plant.CommonName != "Unknown Plant")
//                {
//                    plantsList.Add(plant);
//                }

//                // SAFETY MEASURE: Wait 800 milliseconds before the next request.
//                // This prevents Perenual from throwing a '429 Too Many Requests' error on their free tier.
//                await Task.Delay(800);
//            }

//            return plantsList;
//        }

//        // ORIGINAL METHOD: Fetches a single plant by its specific ID
//        public async Task<PlantCatalog?> FetchPlantByIdAsync(int plantId)
//        {
//            //string url = $"https://perenual.com/api/species-list?key={_Configration["PerenualApiKey"]}&page={page}";
//            string url = $"https://perenual.com/api/species/details/{plantId}?key={_Configration["PerenualApiKey"]}";

//            var response = await _httpClient.GetAsync(url);

//            if (response.IsSuccessStatusCode)
//            {
//                string jsonResponse = await response.Content.ReadAsStringAsync();

//                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
//                JsonElement item = doc.RootElement;

//                // Safely extract text data
//                string commonName = item.TryGetProperty("common_name", out var cn) && cn.ValueKind != JsonValueKind.Null ? cn.GetString() : "Unknown Plant";

//                string scientificName = "Unknown";
//                if (item.TryGetProperty("scientific_name", out var sciElement) && sciElement.GetArrayLength() > 0)
//                {
//                    scientificName = sciElement[0].GetString() ?? "Unknown";
//                }

//                string watering = item.TryGetProperty("watering", out var waterElement) && waterElement.ValueKind != JsonValueKind.Null
//                    ? waterElement.GetString()
//                    : "Medium";

//                string sunlight = "Partial Sun";
//                if (item.TryGetProperty("sunlight", out var sunElement) && sunElement.ValueKind == JsonValueKind.Array && sunElement.GetArrayLength() > 0)
//                {
//                    sunlight = sunElement[0].GetString() ?? "Partial Sun";
//                }

//                // Create the new catalog item
//                var plant = new PlantCatalog
//                {
//                    Nickname = commonName,
//                    CommonName = commonName,
//                    ScientificName = scientificName,
//                    WaterRequirement = watering,
//                    SunRequirement = sunlight,
//                    Location = "Garden",
//                    HealthStatus = "Healthy",
//                    //Images = new PlantImages() // Initialize the complex type
//                };

//                // --- IMAGE MAPPING TO COMPLEX TYPE ---
//                if (item.TryGetProperty("default_image", out JsonElement imgElement) && imgElement.ValueKind != JsonValueKind.Null)
//                {
//                    plant.OriginalUrl = imgElement.TryGetProperty("original_url", out var orig) && orig.ValueKind != JsonValueKind.Null ? orig.GetString() ?? "" : "";
//                    plant.RegularUrl = imgElement.TryGetProperty("regular_url", out var reg) && reg.ValueKind != JsonValueKind.Null ? reg.GetString() ?? "" : "";
//                    plant.MediumUrl = imgElement.TryGetProperty("medium_url", out var med) && med.ValueKind != JsonValueKind.Null ? med.GetString() ?? "" : "";
//                    plant.SmallUrl = imgElement.TryGetProperty("small_url", out var sm) && sm.ValueKind != JsonValueKind.Null ? sm.GetString() ?? "" : "";
//                    plant.Thumbnail = imgElement.TryGetProperty("thumbnail", out var thumb) && thumb.ValueKind != JsonValueKind.Null ? thumb.GetString() ?? "" : "";

//                    // Fallback for your UI that still uses the base ImageUrl property
//                    plant.ImageUrl = plant.RegularUrl;
//                }


//                return plant;
//            }

//            // Return null if API call fails (e.g., ID doesn't exist, 404, or rate limit hit)
//            return null;
//        }
//    }
//}

//using LeafBy.Models;
//using Microsoft.AspNetCore.Hosting;
//using System.Text.Json;

//namespace LeafBy.Data
//{
//    public class PerenualApiService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly IConfiguration _Configration;

//        // 1. Add the web host environment to access the wwwroot folder
//        private readonly IWebHostEnvironment _webHostEnvironment;

//        public PerenualApiService(HttpClient httpClient, IConfiguration Config, IWebHostEnvironment webHostEnvironment)
//        {
//            _httpClient = httpClient;
//            _Configration = Config;
//            _webHostEnvironment = webHostEnvironment;
//        }

//        public async Task<List<PlantCatalog>> FetchPlantsSequentiallyAsync(int startId = 1, int endId = 60)
//        {
//            var plantsList = new List<PlantCatalog>();

//            for (int i = startId; i <= endId; i++)
//            {
//                var plant = await FetchPlantByIdAsync(i);

//                if (plant != null && plant.CommonName != "Unknown Plant")
//                {
//                    plantsList.Add(plant);
//                }

//                // Wait 800ms to avoid API limits
//                await Task.Delay(800);
//            }

//            return plantsList;
//        }

//        public async Task<PlantCatalog?> FetchPlantByIdAsync(int plantId)
//        {
//            string url = $"https://perenual.com/api/species/details/{plantId}?key={_Configration["PerenualApiKey"]}";
//            var response = await _httpClient.GetAsync(url);


//            // NEW: If the API says "Too Many Requests", wait 10 seconds and try exactly one more time.
//            //if ((int)response.StatusCode == 429)
//            //{
//            //    Console.WriteLine($"Rate limit hit on Plant ID {plantId}. Waiting 10 seconds...");
//            //    await Task.Delay(10000);
//            //    response = await _httpClient.GetAsync(url); // Retry
//            //}

//            if (response.IsSuccessStatusCode)
//            {
//                string jsonResponse = await response.Content.ReadAsStringAsync();
//                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
//                JsonElement item = doc.RootElement;

//                string commonName = item.TryGetProperty("common_name", out var cn) && cn.ValueKind != JsonValueKind.Null ? cn.GetString() : "Unknown Plant";

//                string scientificName = "Unknown";
//                if (item.TryGetProperty("scientific_name", out var sciElement) && sciElement.GetArrayLength() > 0)
//                {
//                    scientificName = sciElement[0].GetString() ?? "Unknown";
//                }

//                string watering = item.TryGetProperty("watering", out var waterElement) && waterElement.ValueKind != JsonValueKind.Null
//                    ? waterElement.GetString() : "Medium";

//                string sunlight = "Partial Sun";
//                if (item.TryGetProperty("sunlight", out var sunElement) && sunElement.ValueKind == JsonValueKind.Array && sunElement.GetArrayLength() > 0)
//                {
//                    sunlight = sunElement[0].GetString() ?? "Partial Sun";
//                }

//                var plant = new PlantCatalog
//                {
//                    Nickname = commonName,
//                    CommonName = commonName,
//                    ScientificName = scientificName,
//                    WaterRequirement = watering,
//                    SunRequirement = sunlight,
//                    Location = "Garden",
//                    HealthStatus = "Healthy"
//                };

//                // --- IMAGE DOWNLOADING AND MAPPING ---
//                if (item.TryGetProperty("default_image", out JsonElement imgElement) && imgElement.ValueKind != JsonValueKind.Null)
//                {
//                    // Get the raw URLs from the API
//                    string orig = imgElement.TryGetProperty("original_url", out var o) && o.ValueKind != JsonValueKind.Null ? o.GetString() ?? "" : "";
//                    string reg = imgElement.TryGetProperty("regular_url", out var r) && r.ValueKind != JsonValueKind.Null ? r.GetString() ?? "" : "";
//                    string med = imgElement.TryGetProperty("medium_url", out var m) && m.ValueKind != JsonValueKind.Null ? m.GetString() ?? "" : "";
//                    string sm = imgElement.TryGetProperty("small_url", out var s) && s.ValueKind != JsonValueKind.Null ? s.GetString() ?? "" : "";
//                    string thumb = imgElement.TryGetProperty("thumbnail", out var t) && t.ValueKind != JsonValueKind.Null ? t.GetString() ?? "" : "";

//                    // Pass the URLs to our new download method. 
//                    // It will return the local path (e.g. /images/plants/plant_1_regular.jpg)
//                    plant.OriginalUrl = await DownloadAndSaveImageAsync(orig, $"plant_{plantId}_original.jpg");
//                    plant.RegularUrl = await DownloadAndSaveImageAsync(reg, $"plant_{plantId}_regular.jpg");
//                    plant.MediumUrl = await DownloadAndSaveImageAsync(med, $"plant_{plantId}_medium.jpg");
//                    plant.SmallUrl = await DownloadAndSaveImageAsync(sm, $"plant_{plantId}_small.jpg");
//                    plant.Thumbnail = await DownloadAndSaveImageAsync(thumb, $"plant_{plantId}_thumbnail.jpg");

//                    // Fallback for UI
//                    plant.ImageUrl = plant.RegularUrl;
//                }

//                return plant;
//            }

//            return null;
//        }

//        // 2. NEW HELPER METHOD: Downloads the image and returns the local relative path
//        private async Task<string> DownloadAndSaveImageAsync(string imageUrl, string fileName)
//        {
//            // If the API didn't provide a URL, just return empty
//            if (string.IsNullOrWhiteSpace(imageUrl)) return "";

//            try
//            {
//                // Define the physical path on your server: wwwroot/images/plants
//                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "plants");

//                // Ensure the folder actually exists before trying to save to it
//                Directory.CreateDirectory(uploadsFolder);

//                // Combine the folder path with the specific file name
//                string fullFilePath = Path.Combine(uploadsFolder, fileName);

//                // Download the image data
//                byte[] imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);

//                // Write the image data to your server's hard drive
//                await File.WriteAllBytesAsync(fullFilePath, imageBytes);

//                // Return the relative URL so the browser can load it using <img src="..." />
//                return $"/images/plants/{fileName}";
//            }
//            catch (Exception ex)
//            {
//                // If the download fails (e.g. broken link from the API), log it and return the original URL as a fallback
//                Console.WriteLine($"Error downloading image {imageUrl}: {ex.Message}");
//                return imageUrl;
//            }
//        }
//    }
//}




//using LeafBy.Models;
//using Microsoft.AspNetCore.Hosting;
//using System.Text.Json;

//namespace LeafBy.Data
//{
//    public class PerenualApiService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly IConfiguration _Configration;
//        private readonly IWebHostEnvironment _webHostEnvironment;

//        public PerenualApiService(HttpClient httpClient, IConfiguration Config, IWebHostEnvironment webHostEnvironment)
//        {
//            _httpClient = httpClient;
//            _Configration = Config;
//            _webHostEnvironment = webHostEnvironment;
//        }

//        // =========================================================================
//        // NEW BATCH METHOD: Fetches 30 plants per page in one single API call
//        // =========================================================================
//        public async Task<List<PlantCatalog>> FetchPlantsInBatchAsync(int startPage = 1, int endPage = 2)
//        {
//            var plantsList = new List<PlantCatalog>();

//            for (int page = startPage; page <= endPage; page++)
//            {
//                // Notice we use your injected Config for the key to keep it secure
//                string url = $"https://perenual.com/api/v2/species-list?key={_Configration["PerenualApiKey"]}&page={page}";

//                var response = await _httpClient.GetAsync(url);

//                if (response.IsSuccessStatusCode)
//                {
//                    string jsonResponse = await response.Content.ReadAsStringAsync();
//                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);

//                    // The 'species-list' endpoint wraps the plants in a "data" array
//                    if (doc.RootElement.TryGetProperty("data", out JsonElement dataArray) && dataArray.ValueKind == JsonValueKind.Array)
//                    {
//                        // Loop through all 30 plants on this page
//                        foreach (JsonElement item in dataArray.EnumerateArray())
//                        {
//                            // Get the Perenual ID (used for naming the image later)
//                            int plantId = item.TryGetProperty("id", out var idEl) && idEl.ValueKind != JsonValueKind.Null ? idEl.GetInt32() : new Random().Next(1000, 9999);

//                            string commonName = item.TryGetProperty("common_name", out var cn) && cn.ValueKind != JsonValueKind.Null ? cn.GetString() : "Unknown Plant";

//                            // Skip junk entries
//                            if (string.IsNullOrWhiteSpace(commonName) || commonName == "Unknown Plant") continue;

//                            string scientificName = "Unknown";
//                            if (item.TryGetProperty("scientific_name", out var sciElement) && sciElement.GetArrayLength() > 0)
//                            {
//                                scientificName = sciElement[0].GetString() ?? "Unknown";
//                            }

//                            string watering = item.TryGetProperty("watering", out var waterElement) && waterElement.ValueKind != JsonValueKind.Null
//                                ? waterElement.GetString() : "Medium";

//                            string sunlight = "Partial Sun";
//                            if (item.TryGetProperty("sunlight", out var sunElement) && sunElement.ValueKind == JsonValueKind.Array && sunElement.GetArrayLength() > 0)
//                            {
//                                sunlight = sunElement[0].GetString() ?? "Partial Sun";
//                            }

//                            var plant = new PlantCatalog
//                            {
//                                Nickname = commonName,
//                                CommonName = commonName,
//                                ScientificName = scientificName,
//                                WaterRequirement = watering,
//                                SunRequirement = sunlight,
//                                Location = "Garden",
//                                HealthStatus = "Healthy"
//                            };

//                            // --- IMAGE DOWNLOADING ---
//                            if (item.TryGetProperty("default_image", out JsonElement imgElement) && imgElement.ValueKind != JsonValueKind.Null)
//                            {
//                                string orig = imgElement.TryGetProperty("original_url", out var o) && o.ValueKind != JsonValueKind.Null ? o.GetString() ?? "" : "";
//                                string reg = imgElement.TryGetProperty("regular_url", out var r) && r.ValueKind != JsonValueKind.Null ? r.GetString() ?? "" : "";
//                                string med = imgElement.TryGetProperty("medium_url", out var m) && m.ValueKind != JsonValueKind.Null ? m.GetString() ?? "" : "";
//                                string sm = imgElement.TryGetProperty("small_url", out var s) && s.ValueKind != JsonValueKind.Null ? s.GetString() ?? "" : "";
//                                string thumb = imgElement.TryGetProperty("thumbnail", out var t) && t.ValueKind != JsonValueKind.Null ? t.GetString() ?? "" : "";

//                                // PERFORMANCE SAVER: Only download the Regular image to your hard drive.
//                                // Save the others as raw text strings to prevent hitting rate limits while parsing 30 plants.
//                                plant.RegularUrl = await DownloadAndSaveImageAsync(reg, $"plant_{plantId}_regular.jpg");
//                                plant.ImageUrl = plant.RegularUrl;

//                                plant.OriginalUrl = orig;
//                                plant.MediumUrl = med;
//                                plant.SmallUrl = sm;
//                                plant.Thumbnail = thumb;
//                            }

//                            plantsList.Add(plant);
//                        }
//                    }
//                }

//                // Wait 1.5 seconds between PAGE fetches (not item fetches) to keep the API happy!
//                await Task.Delay(1500);
//            }

//            return plantsList;
//        }


//        // =========================================================================
//        // HELPER METHOD: Downloads the image and returns the local relative path
//        // =========================================================================
//        // =========================================================================
//        // HELPER METHOD: Safely downloads the image and handles broken (404) links
//        // =========================================================================
//        private async Task<string> DownloadAndSaveImageAsync(string imageUrl, string fileName)
//        {
//            // If the API didn't provide a URL, just return empty
//            if (string.IsNullOrWhiteSpace(imageUrl)) return "";

//            try
//            {
//                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "plants");
//                Directory.CreateDirectory(uploadsFolder);
//                string fullFilePath = Path.Combine(uploadsFolder, fileName);

//                // 1. Send a request to the image URL (but don't force a download yet)
//                var response = await _httpClient.GetAsync(imageUrl);

//                // 2. Safely check if the image actually exists on their server
//                if (response.IsSuccessStatusCode)
//                {
//                    // The image exists! Download the bytes and save it.
//                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
//                    await File.WriteAllBytesAsync(fullFilePath, imageBytes);
//                    return $"/images/plants/{fileName}";
//                }
//                else
//                {
//                    // The API gave us a broken link (404 Not Found).
//                    // Do NOT throw an error. Just log it and return the external URL as a fallback.
//                    Console.WriteLine($"API provided a broken image link ({response.StatusCode}) for: {imageUrl}");
//                    return imageUrl;
//                }
//            }
//            catch (Exception ex)
//            {
//                // Catch any other network timeouts or weird errors
//                Console.WriteLine($"Network error downloading image {imageUrl}: {ex.Message}");
//                return imageUrl;
//            }
//        }
//    }
//}



using LeafBy.Models;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;

namespace LeafBy.Data
{
    public class PerenualApiService // Kept the same name to avoid breaking your DI!
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _Configration;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PerenualApiService(HttpClient httpClient, IConfiguration Config, IWebHostEnvironment webHostEnvironment)
        {
            _httpClient = httpClient;
            _Configration = Config;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<List<PlantCatalog>> FetchPlantsInBatchAsync()
        {
            var plantsList = new List<PlantCatalog>();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://house-plants2.p.rapidapi.com/all-lite"),
                Headers =
                {
                    { "X-RapidAPI-Key", _Configration["RapidApiKey"] },
                    { "X-RapidAPI-Host", "house-plants2.p.rapidapi.com" },
                }
            };

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);

                // This API returns a direct array of plants
                int count = 0;
                foreach (JsonElement item in doc.RootElement.EnumerateArray())
                {
                    // Grab 60 plants to match your previous import batch size
                    if (count >= 60) break;

                    string commonName = item.TryGetProperty("Common name", out var cn) && cn.ValueKind == JsonValueKind.Array && cn.GetArrayLength() > 0
                        ? cn[0].GetString() : "Unknown Plant";

                    if (string.IsNullOrWhiteSpace(commonName) || commonName == "Unknown Plant") continue;

                    string scientificName = item.TryGetProperty("Latin name", out var sci) ? sci.GetString() : "Unknown";
                    string watering = item.TryGetProperty("Watering", out var water) ? water.GetString() : "Medium";
                    string sunlight = item.TryGetProperty("Light ideal", out var sun) ? sun.GetString() : "Partial Sun";
                    string imageUrl = item.TryGetProperty("Img", out var img) ? img.GetString() : "";

                    var plant = new PlantCatalog
                    {
                        Nickname = commonName,
                        CommonName = commonName,
                        ScientificName = scientificName,
                        WaterRequirement = watering,
                        SunRequirement = sunlight,
                        Location = "Garden",
                        HealthStatus = "Healthy"
                    };

                    // Download the real, unblocked image
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        // Generate a safe filename removing spaces
                        string safeName = commonName.Replace(" ", "_").ToLower();
                        string localPath = await DownloadAndSaveImageAsync(imageUrl, $"{safeName}_img.jpg");

                        plant.ImageUrl = localPath;
                        plant.RegularUrl = localPath;
                    }

                    plantsList.Add(plant);
                    count++;
                }
            }

            return plantsList;
        }

        private async Task<string> DownloadAndSaveImageAsync(string imageUrl, string fileName)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return "";

            try
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "plants");
                Directory.CreateDirectory(uploadsFolder);
                string fullFilePath = Path.Combine(uploadsFolder, fileName);

                var response = await _httpClient.GetAsync(imageUrl);

                if (response.IsSuccessStatusCode)
                {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(fullFilePath, imageBytes);
                    return $"/images/plants/{fileName}";
                }

                return imageUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Network error downloading image {imageUrl}: {ex.Message}");
                return imageUrl;
            }
        }
    }
}