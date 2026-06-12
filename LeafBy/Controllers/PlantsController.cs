using LeafBy.Data;
using LeafBy.Models;
using LeafBy.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace LeafBy.Controllers
{
    [Authorize]
    public class PlantsController : Controller
    {
        private readonly ILogger<PlantsController> _logger; // Fixed logger type
        private readonly ApplicationDbContext _DB;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IWebHostEnvironment _env;

        public PlantsController(ILogger<PlantsController> logger, ApplicationDbContext db, IConfiguration configuration, IWebHostEnvironment webHostEnvironment, IWebHostEnvironment env)
        {
            _logger = logger;
            _DB = db;
            _configuration = configuration;
            _httpClient = new HttpClient();
            _webHostEnvironment = webHostEnvironment;
            _env = env;
        }

        // Updated to accept an Ecosystem type
        public class GardenTask
        {
            public int id { get; set; }
            public string Ecosystem { get; set; } = "Soil"; // e.g., "Soil" or "Aquatic"
        }

        public IActionResult Index()
        {

            // 1. Grab the currently logged-in user's ID
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Safety check: Ensure they are actually logged in
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var existingUser = _DB.Users.OfType<Appuser>().FirstOrDefault(x => x.Id == currentUserId);

            var homeViewModel = new HomeViewModel()
            {
                AppUser = new Appuser(),
                PlantsCatalog = _DB.PlantCatalog.ToList()
            };
            homeViewModel.AppUser.Id = currentUserId;
            homeViewModel.AppUser.MyPlants = _DB.Plants.Where(x => x.AppUserId == currentUserId).ToList();
            homeViewModel.AppUser.MyTasks = _DB.CalendarTasks.Where(x => x.AppUserId == currentUserId).ToList();
            homeViewModel.AppUser.UserName = User.Identity?.Name ?? existingUser?.UserName;

            // 3. Safely pull from session, fallback to database, fallback to "0" if null
            string sessionCity = HttpContext.Session.GetString("UserCity") ?? existingUser?.City ?? "Unknown";
            string sessionCountry = HttpContext.Session.GetString("UserCountry") ?? existingUser?.country ?? "Unknown";
            string sessionLat = HttpContext.Session.GetString("UserLat") ?? existingUser?.lat ?? "0";
            string sessionLon = HttpContext.Session.GetString("UserLon") ?? existingUser?.lon ?? "0";

            homeViewModel.AppUser.City = sessionCity;
            homeViewModel.AppUser.country = sessionCountry;
            homeViewModel.AppUser.lat = sessionLat;
            homeViewModel.AppUser.lon = sessionLon;

            // Convert string coordinates safely to doubles to prevent application crashes
            double.TryParse(sessionLat, out double userLat);
            double.TryParse(sessionLon, out double userLon);

            // 4. Fetch OTHER people's listings for the Community Hub
            var otherPeoplesListings = _DB.CommunityListings
                                          .ToList();


            // Sort by closest distance and assign to view model
            homeViewModel.AppUser.MyListings = otherPeoplesListings.OrderBy(l => l.DistanceMiles).ToList();


            // Fetch Incoming Requests (People wanting YOUR plants)
            homeViewModel.IncomingRequests = _DB.ListingRequests
                .Where(r => r.OwnerId == currentUserId)
                .OrderByDescending(r => r.DateRequested)
                .ToList();

            // Fetch Outgoing Requests (Plants YOU asked for)
            homeViewModel.OutgoingRequests = _DB.ListingRequests
                .Include(r => r.Listing)
                .Where(r => r.RequesterId == currentUserId)
                .OrderByDescending(r => r.DateRequested)
                .ToList();
            // Removed _DB.SaveChanges() here because we are only reading data, not updating the database!
            return View(homeViewModel);
        }

        [HttpPost]
        public IActionResult AddToGarden([FromBody] GardenTask gardenTask)
        {
            var plant = _DB.PlantCatalog.FirstOrDefault(x => x.Id.Equals(gardenTask.id));
            if (plant == null) return Json(new { success = false, message = "Catalog plant not found." });

            string currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var existingMyPlants = _DB.Plants.Where(x => x.AppUserId.Equals(currentUserId)).ToList();

            bool inMyPlant = existingMyPlants.Any(x => x.CommonName.Equals(plant.CommonName));
            if (inMyPlant || gardenTask.id.Equals(0))
            {
                return Json(new { success = false, message = "Plant already exists in your garden!" });
            }

            var plantToBeAdded = new Plant()
            {
                //Id = plant.Id,
                AppUserId = currentUserId,
                CommonName = plant.CommonName,
                HealthStatus = plant.HealthStatus,
                ScientificName = plant.ScientificName,
                ImageUrl = plant.ImageUrl,
                Location = gardenTask.Ecosystem, // Storing if it's an Aquatic or Soil plant
                Nickname = plant.Nickname,
                SunRequirement = plant.SunRequirement,
                WaterRequirement = plant.WaterRequirement,
                SoilMix = plant.SoilMix,
                CareProfile = plant.CareProfile,
                HomeRemedies = plant.HomeRemedies

            };
            //plantToBeAdded.Id = null;
            
            _DB.Plants.Add(plantToBeAdded);
            _DB.SaveChanges();

            // UNIQUE FEATURE: Auto-generate tailored tasks based on the Ecosystem
            GenerateEcosystemTasks(plantToBeAdded.Id, currentUserId, gardenTask.Ecosystem);

            return Json(new { success = true, message = "Successfully added to your garden!" });
        }

        [HttpPost]
        public IActionResult RemoveFromGarden([FromBody] GardenTask gardenTask)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "User not authenticated" });

            var plant = _DB.Plants.FirstOrDefault(x => x.Id.Equals(gardenTask.id));

            if (plant is null)
                return Json(new { success = false, message = "Plant not found" });

            if (!plant.AppUserId.Equals(userId))
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                _DB.Plants.Remove(plant);
                var tasks = _DB.CalendarTasks.Where(x => x.MyPlantId.Equals(plant.Id));
                _DB.CalendarTasks.RemoveRange(tasks);
                _DB.SaveChanges();

                return Json(new { success = true, message = "Plant removed successfully" });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(new { success = false, message = "Plant was already deleted" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchLocalPlants(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<object>());
            }

            string cleanQuery = query.ToLower().Trim();

            // 1. Get the current logged-in user's ID
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Search the Global Catalog
            var catalogResults = await _DB.PlantCatalog
                .Where(p => (p.CommonName != null && p.CommonName.ToLower().Contains(cleanQuery)) ||
                            (p.ScientificName != null && p.ScientificName.ToLower().Contains(cleanQuery)) ||
                            (p.Nickname != null && p.Nickname.ToLower().Contains(cleanQuery)))
                .Select(p => new
                {
                    id = p.Id,
                    title = p.CommonName,
                    subtitle = p.ScientificName,
                    image = p.ImageUrl,
                    water = p.WaterRequirement,
                    sun = p.SunRequirement,
                    isPersonal = false // Flag to let your UI know it's a global catalog plant
                })
                .Take(10)
                .ToListAsync();

            // 3. Search the User's Personal Plants (if they are logged in)
            var userResults = new List<dynamic>();

            if (!string.IsNullOrEmpty(currentUserId))
            {
                var personalPlants = await _DB.Plants
                    .Where(p => p.AppUserId == currentUserId &&
                               ((p.CommonName != null && p.CommonName.ToLower().Contains(cleanQuery)) ||
                                (p.ScientificName != null && p.ScientificName.ToLower().Contains(cleanQuery)) ||
                                (p.Nickname != null && p.Nickname.ToLower().Contains(cleanQuery))))
                    .Select(p => new
                    {
                        id = p.Id,
                        title = p.CommonName,
                        subtitle = p.ScientificName,
                        image = p.ImageUrl,
                        water = p.WaterRequirement,
                        sun = p.SunRequirement,
                        isPersonal = true // Flag to let your UI know it's their personal plant
                    })
                    .Take(10)
                    .ToListAsync();

                userResults.AddRange(personalPlants);
            }

            // 4. Combine the results, prioritize personal plants first, limit to 10 total
            var combinedResults = userResults
                .Concat(catalogResults)
                .Take(10)
                .ToList();

            return Json(combinedResults);
        }

        // --- UNIQUE FEATURES ---

        // 1. Ecosystem Task Generator
        private void GenerateEcosystemTasks(int personalPlantId, string userId, string ecosystem)
        {
            var tasks = new List<CalenderTask>();

            if (ecosystem == "Aquatic")
            {
                tasks.Add(new CalenderTask { AppUserId = userId, MyPlantId = personalPlantId, TaskName = "Check Pond Filter", Category = "Maintenance", TaskDate = DateTime.UtcNow.AddDays(7) });
                tasks.Add(new CalenderTask { AppUserId = userId, MyPlantId = personalPlantId, TaskName = "Feed Molly/Platy Fish", Category = "Feeding", TaskDate = DateTime.UtcNow.AddDays(1) });
                tasks.Add(new CalenderTask { AppUserId = userId, MyPlantId = personalPlantId, TaskName = "Inspect Lotus Root Growth", Category = "Maintenance", TaskDate = DateTime.UtcNow.AddDays(14) });
            }
            else
            {
                tasks.Add(new CalenderTask { AppUserId = userId, MyPlantId = personalPlantId, TaskName = "Check Soil Moisture", Category = "Water", TaskDate = DateTime.UtcNow.AddDays(2) });
                tasks.Add(new CalenderTask { AppUserId = userId, MyPlantId = personalPlantId, TaskName = "Apply Liquid Fertilizer", Category = "Fertilize", TaskDate = DateTime.UtcNow.AddDays(14) });
            }

            _DB.CalendarTasks.AddRange(tasks);
            _DB.SaveChanges();
        }
        // GET: /Plants/Details/1
        [HttpGet]
   public async Task<IActionResult> Details(int id, bool isMyPlant = false)
{
    var viewModel = new PlantDetailViewModel();

    if (isMyPlant)
    {
        // ==========================================
        // 1. LOGIC FOR MY PLANTS (User's Garden)
        // ==========================================
        var userPlant = await _DB.Plants
            .Include(p => p.SoilMix)
            .Include(p => p.HomeRemedies)
            .Include(p => p.CareProfile)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (userPlant == null) return NotFound();

        // Map to ViewModel's expected Catalog shape
        viewModel.Plant = new PlantCatalog
        {
            Id = userPlant.Id,
            CommonName = userPlant.CommonName ?? "Unknown Plant",
            ScientificName = userPlant.ScientificName ?? "",
            ImageUrl = userPlant.ImageUrl,
            SoilMix = userPlant.SoilMix,
            CareProfile = userPlant.CareProfile,
            HomeRemedies = userPlant.HomeRemedies ?? new List<HomeRemdy>()
        };

        viewModel.CareProfile = userPlant.CareProfile ?? new PlantCareProfile();
        viewModel.Soil = userPlant.SoilMix ?? new SoilMix();
    }
    else
    {
        // ==========================================
        // 2. LOGIC FOR PLANT CATALOG (Encyclopedia)
        // ==========================================
        var catalogPlant = await _DB.PlantCatalog
            .Include(p => p.SoilMix)
            .Include(p => p.HomeRemedies)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (catalogPlant == null) return NotFound();

        var careProfile = await _DB.PlantCareProfiles
            .FirstOrDefaultAsync(c => c.PlantCatalogId == id) ?? new PlantCareProfile();

        viewModel.Plant = catalogPlant;
        viewModel.CareProfile = careProfile;
        viewModel.Soil = catalogPlant.SoilMix ?? new SoilMix();
    }

    // ==========================================
    // 3. SHARED LOGIC (Remedies Mapping)
    // ==========================================
    var remedies = viewModel.Plant.HomeRemedies ?? new List<HomeRemdy>();

    viewModel.CommercialFertilizer = remedies.FirstOrDefault(r => r.Type == "Fertilizer" || r.Type == "Commercial") ?? new HomeRemdy();
    viewModel.OrganicRemedy = remedies.FirstOrDefault(r => r.Type == "Organic") ?? new HomeRemdy();

    // Fallback if no organic remedy is found
    if (string.IsNullOrEmpty(viewModel.OrganicRemedy.Title))
    {
        viewModel.OrganicRemedy = remedies.FirstOrDefault(x => x.Title != null) ?? new HomeRemdy();
    }

    return View(viewModel);
}     // 2. Smart Climate Predictor
        [HttpGet]
        public async Task<IActionResult> GetSmartClimateAdvice()
        {
            string apiKey = _configuration["OpenWeatherApiKey"];
            string lat = HttpContext.Session.GetString("UserLat") ?? "30.9010"; // Fallbacks
            string lon = HttpContext.Session.GetString("UserLon") ?? "75.8573";

            string apiUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&units=metric&appid={apiKey}";
            var response = await _httpClient.GetAsync(apiUrl);

            string advice = "Keep up the great gardening!";

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(responseString);
                double tempDouble = doc.RootElement.GetProperty("main").GetProperty("temp").GetDouble();
                int temp = (int)Math.Round(tempDouble);

                // Smart local forecasting logic
                if (temp >= 35)
                {
                    advice = $"It's {temp}°C outside. Protect your terrace pots from intense midday sun and top off any outdoor water features.";
                }
                else if (temp <= 18)
                {
                    advice = $"Cooler weather detected ({temp}°C). Great time to prep seasonal blooms like California poppies or sow winter garlic.";
                }
                else
                {
                    advice = $"Perfect growing weather at {temp}°C! Monitor your soil moisture and enjoy the blooms.";
                }
            }

            return Json(new { advice = advice });
        }



        // 2. The Action Method: This catches the form submission from your Modal
        [HttpPost]
        public async Task<IActionResult> AddUserPlant(Plant model, IFormFile plantImage, string Base64Image, string RawAiJson)
        {
            // 1. LINK TO THE LOGGED-IN USER
            // This ensures it goes to "My Plants" for whoever is currently logged in

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                // Prevent the crash if someone tries to save while logged out
                TempData["Error"] = "You must be logged in to save a plant to your garden.";
                return RedirectToAction("Index", "Home");
            }

            model.AppUserId = currentUserId; // This passes the correct GUID to SQL Server!
            model.HealthStatus = "Healthy";

            // 2. PROCESS THE IMAGE (With our bulletproof cleaner)
            if (!string.IsNullOrWhiteSpace(Base64Image))
            {
                try
                {
                    // 1. Clean the Base64 string
                    if (Base64Image.Contains(","))
                        Base64Image = Base64Image.Substring(Base64Image.IndexOf(",") + 1);

                    Base64Image = Base64Image.Replace(" ", "+").Trim();

                    // 2. Format as a Data URI
                    // This variable 'model.ImageUrl' now holds the image regardless of the environment
                    model.ImageUrl = $"data:image/jpeg;base64,{Base64Image}";
                }
                catch (Exception ex)
                {
                    // Fallback for any environment
                    model.ImageUrl = "https://placehold.co/150x150/12372A/ffffff?text=Image+Error";
                }
            }
            else
            {
                // Fallback for any environment
                model.ImageUrl = "https://placehold.co/150x150/12372A/ffffff?text=No+Photo";
            }
            // 3. MAP THE AI DATA TO THIS SPECIFIC PHYSICAL PLANT
            if (!string.IsNullOrWhiteSpace(RawAiJson))
            {
                var aiData = JsonSerializer.Deserialize<PlantScanResult>(
                    RawAiJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (aiData != null)
                {
                    model.ScientificName = aiData.ScientificName ?? model.ScientificName;
                    model.SunRequirement = aiData.Sunlight?.Amount ?? "Partial Sun";
                    model.WaterRequirement = aiData.Watering?.Amount ?? "Medium";

                    // Inside the aiData mapping block
                    if (aiData.SoilMix != null && aiData.SoilMix.Count > 0)
                    {
                        model.SoilMix = new SoilMix
                        {
                            PlantCatalogId = null, // <--- ADD THIS LINE! Force SQL to leave it blank.
                            Description = aiData.SoilNote ?? "",
                            Component1 = aiData.SoilMix.ElementAtOrDefault(0)?.Name ?? "",
                            Percentage1 = aiData.SoilMix.ElementAtOrDefault(0)?.Percentage ?? 0,
                            Component2 = aiData.SoilMix.ElementAtOrDefault(1)?.Name ?? "",
                            Percentage2 = aiData.SoilMix.ElementAtOrDefault(1)?.Percentage ?? 0,
                            Component3 = aiData.SoilMix.ElementAtOrDefault(2)?.Name ?? "",
                            Percentage3 = aiData.SoilMix.ElementAtOrDefault(2)?.Percentage ?? 0
                        };
                    }
                    // If a pest was found, attach the remedy directly to THIS plant so the user can treat it
                    if (aiData.Treatment?.Organic != null)
                    {
                        model.HealthStatus = "Needs Attention"; // Update health status if a pest is found

                        model.HomeRemedies.Add(new HomeRemdy
                        {
                            Type = "Organic Pest Control",
                            Title = aiData.PestName ?? "Organic Treatment",
                            Description = "Threat Level: " + aiData.ThreatLevel,
                            Instructions = aiData.Treatment.Organic
                        });
                    }
                    // If the AI generated a preventative care home remedy for a healthy plant
                    if (aiData.HomeRemedy != null)
                    {
                        model.HomeRemedies.Add(new HomeRemdy
                        {
                            Type = aiData.HomeRemedy.Type ?? "Organic Care",
                            Title = aiData.HomeRemedy.Title ?? "Plant Care Hack",
                            Description = aiData.HomeRemedy.Description ?? "",
                            Instructions = aiData.HomeRemedy.Instructions ?? ""
                        });
                    }
                    // Inside the RawAiJson mapping block in AddUserPlant
                    if (aiData != null)
                    {
                        // ... existing basic property mapping ...

                        // Map the highly detailed Care Profile
                        model.CareProfile = new PlantCareProfile
                        {
                            SunlightDetails = aiData.Sunlight?.Detail ?? "",
                            WaterDetails = aiData.Watering?.Detail ?? "",
                            PruningRequirement = aiData.Pruning?.Amount ?? "",
                            PruningDetails = aiData.Pruning?.Detail ?? "",
                            ThermalSensitivity = aiData.ThermalSensitivity ?? ""
                        };

                        // ... existing Soil Mix and Home Remedy mapping ...
                    }
                    // If a pest was found, attach the remedy directly to THIS plant
                    if (aiData.Treatment?.Organic != null)
                    {
                        model.HealthStatus = "Needs Attention";

                        model.HomeRemedies.Add(new HomeRemdy
                        {
                            PlantCatalogId = null, // FORCE SQL to leave the catalog column blank
                            Type = "Organic Pest Control",
                            Title = aiData.PestName ?? "Organic Treatment",
                            Description = "Threat Level: " + aiData.ThreatLevel,
                            Instructions = aiData.Treatment.Organic
                            
                        });
                    }
                }
            }
            Console.WriteLine(model);
            // 4. SAVE TO THE USER'S PERSONAL INVENTORY
            // This is the crucial change: saving to _context.Plants instead of PlantCatalogs
            _DB.Plants.Add(model);
            await _DB.SaveChangesAsync();

            TempData["Success"] = $"{model.CommonName} has been successfully added to your personal garden!";

            // Redirect to the "My Plants" dashboard page!
            return RedirectToAction("Index", "Home");
        }
    }
}