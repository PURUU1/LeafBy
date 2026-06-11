using LeafBy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory; // Required for IMemoryCache
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography; // Required for SHA256 Hash
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafBy.Controllers
{
    public class PlantScannerController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        // FIXED: Added IMemoryCache to the constructor parameters so it actually injects
        public PlantScannerController(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            _cache = cache;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UploadToSession(IFormFile scanImage, string category)
        {
            if (scanImage != null && scanImage.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    scanImage.CopyTo(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();

                    HttpContext.Session.Set("CapturedScan", imageBytes);
                    HttpContext.Session.SetString("ScanCategory", category);

                    return Json(new { success = true, redirectUrl = Url.Action("AnalyzeIssue", "PlantScanner") });
                }
            }

            return Json(new { success = false, message = "No image data received." });
        }

        [HttpGet]
        public async Task<IActionResult> AnalyzeIssue()
        {
            byte[] imageBytes = HttpContext.Session.Get("CapturedScan");
            string category = HttpContext.Session.GetString("ScanCategory") ?? "Plant";

            if (imageBytes == null)
            {
                TempData["Error"] = "Session expired or no image found. Please scan again.";
                return RedirectToAction("Index", "Home");
            }

            string base64Image = Convert.ToBase64String(imageBytes);

            // --- 1. CHECK CACHE FIRST BEFORE CALLING API ---
            string imageHash = GenerateImageHash(imageBytes);
            string cacheKey = $"Scan_{category}_{imageHash}";

            if (_cache.TryGetValue(cacheKey, out string cachedJson))
            {
                try
                {
                    // If we found it in cache, deserialize instantly and skip the API call!
                    PlantScanResult cachedResult = JsonSerializer.Deserialize<PlantScanResult>(
                        cachedJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    cachedResult.ScanCategory = category;
                    ViewBag.ImageBase64 = base64Image;
                    return View("Result", cachedResult);
                }
                catch
                {
                    // If the cache was somehow corrupted, silently fail and fall through to a fresh API call
                    _cache.Remove(cacheKey);
                }
            }
            // ------------------------------------------------


            // 2. SWITCH TO NVIDIA NIM CREDENTIALS
            string apiKey = _configuration["NvidiaApiKey"];
            string apiUrl = "https://integrate.api.nvidia.com/v1/chat/completions";

            string promptText = "";

            switch (category)
            {
                case "Pest":
                    promptText = @"You are a master botanist and entomologist. First, identify the plant. Then, identify the pest on it.
                You MUST respond ONLY with a valid, raw JSON object matching this exact schema. Do not add markdown formatting.
                {
                    ""plantName"": ""Common Name"",
                    ""scientificName"": ""Scientific Name"",
                    ""pestName"": ""Name of Pest"",
                    ""threatLevel"": ""Low, Medium, or High"",
                    ""treatment"": {
                        ""commercial"": ""Commercial pesticide"",
                        ""organic"": ""Home remedy name and exact recipe""
                    },
                    ""soilNote"": ""Ideal soil condition""
                }";
                    break;

                case "Disease":
                    promptText = @"You are a master botanist and plant pathologist. First, identify the plant. Then, diagnose the disease or deficiency.
                You MUST respond ONLY with a valid, raw JSON object matching this exact schema. Do not add markdown formatting.
                {
                    ""plantName"": ""Common Name"",
                    ""scientificName"": ""Scientific Name"",
                    ""diagnosis"": ""Name of disease or deficiency"",
                    ""severity"": ""Low, Medium, or High"",
                    ""symptoms"": ""Brief description"",
                    ""actionPlan"": [""Step 1"", ""Step 2""],
                    ""soilNote"": ""Ideal soil condition""
                }";
                    break;

                case "Plant":
                default:
                    promptText = @"You are a master botanist. Identify the plant in this image and provide a highly detailed care and soil profile.
    You MUST respond ONLY with a valid, raw JSON object matching this exact schema. Do not add markdown formatting.
    {
        ""plantName"": ""Common Name"",
        ""scientificName"": ""Scientific Name"",
        ""sunlight"": { ""amount"": ""e.g., Partial Sun"", ""detail"": ""e.g., Place in a bright, East-facing window."" },
        ""watering"": { ""amount"": ""e.g., Medium"", ""detail"": ""e.g., Water only when the top 2 inches of soil are dry."" },
        ""pruning"": { ""amount"": ""e.g., Spring/Fall"", ""detail"": ""e.g., Pinch off dead blooms to encourage growth."" },
        ""soilMix"": [
            { ""name"": ""Compost"", ""percentage"": 40 },
            { ""name"": ""Perlite or Sand"", ""percentage"": 30 },
            { ""name"": ""Loam or Garden Soil"", ""percentage"": 30 }
        ],
        ""soilNote"": ""Provide details on required soil drainage and ideal pH levels."",
        ""homeRemedy"": {
            ""type"": ""Organic Nutrition / Preventative Care"",
            ""title"": ""e.g., Banana Peel Potassium Tea"",
            ""description"": ""Explain exactly why this plant benefits from this specific organic remedy."",
            ""instructions"": ""Provide the exact step-by-step recipe and application schedule.""
        },
        ""thermalSensitivity"": ""Explain its temperature tolerance (e.g., Protect from frost below 10°C).""
    }";
                    break;
            }

            // 3. OPENAI/NVIDIA NIM PAYLOAD STRUCTURE
            var payload = new
            {
                model = "meta/llama-3.2-11b-vision-instruct",
                messages = new[]
                {
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "text", text = promptText },
                    new
                    {
                        type = "image_url",
                        image_url = new { url = $"data:image/jpeg;base64,{base64Image}" }
                    }
                }
            }
        },
                max_tokens = 1024,
                temperature = 0.2
            };

            string jsonPayload = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);

            // 4. ATTACH NVIDIA AUTHORIZATION HEADER
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Headers.Add("Accept", "application/json");
            request.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();

                // 5. PARSE OPENAI STYLE RESPONSE
                using JsonDocument doc = JsonDocument.Parse(responseString);
                string generatedText = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content").GetString() ?? "{}";

                // 6. JSON BOUNDARY CLEANER 
                int firstBrace = generatedText.IndexOf('{');
                int lastBrace = generatedText.LastIndexOf('}');

                if (firstBrace != -1 && lastBrace != -1 && lastBrace >= firstBrace)
                {
                    generatedText = generatedText.Substring(firstBrace, (lastBrace - firstBrace) + 1).Trim();
                }

                try
                {
                    PlantScanResult resultData = JsonSerializer.Deserialize<PlantScanResult>(
                        generatedText,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    resultData.ScanCategory = category;
                    ViewBag.ImageBase64 = base64Image;

                    // --- 7. SAVE TO CACHE ON SUCCESS ---
                    // Save for 15 minutes. If they reload within this time, it hits the cache!
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

                    _cache.Set(cacheKey, generatedText, cacheOptions);
                    // -----------------------------------

                    return View("Result", resultData);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine("STRUCTURAL ANALYSIS REJECTION: " + generatedText);
                    TempData["Error"] = "The AI returned invalid formatting. Please try scanning again.";
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                string errorString = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"NVIDIA API ERROR [{response.StatusCode}]: " + errorString);

                TempData["Error"] = $"AI Service Error: {response.StatusCode}. Check console for details.";
                return RedirectToAction("Index", "Home");
            }
        }

        // --- NEW HELPER METHOD FOR CACHING ---
        private string GenerateImageHash(byte[] imageBytes)
        {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(imageBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}