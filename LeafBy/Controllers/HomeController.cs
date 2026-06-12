using LeafBy.Data;
using LeafBy.Models;
using LeafBy.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafBy.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _DB;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db, IConfiguration configuration)
        {
            _logger = logger;
            _DB = db;
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public IActionResult Index()
        {
            // 1. Grab the currently logged-in user's unique ID
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Safety check: If they are not logged in, return an empty guest layout to prevent View crashes
            if (string.IsNullOrEmpty(currentUserId))
            {
                return View(new HomeViewModel()
                {
                    AppUser = new Appuser()
                    {
                        MyListings = new List<ComunityListing>(),
                        MyPlants = new List<Plant>(),
                        MyTasks = new List<CalenderTask>(),
                        UserName = "Guest"
                    },
                    PlantsCatalog = _DB.PlantCatalog.ToList()
                });
            }

            
            var existingUser = _DB.Users.FirstOrDefault(x => x.Id == currentUserId);

                var homeViewModel = new HomeViewModel()
                {
                    AppUser = new Appuser(),
                    PlantsCatalog = _DB.PlantCatalog.ToList()
                };
                homeViewModel.AppUser.MyListings = _DB.CommunityListings.Where(x => x.AppUserId != currentUserId).ToList();
            homeViewModel.AppUser.MyPlants = _DB.Plants.Where(x => x.AppUserId == currentUserId).ToList();
            homeViewModel.AppUser.MyTasks = _DB.CalendarTasks.Where(x => x.AppUserId == currentUserId).ToList();
            homeViewModel.AppUser.UserName = User.Identity?.Name ?? existingUser.UserName;




            // Fetch Incoming Requests (People wanting YOUR plants)
            homeViewModel.IncomingRequests = _DB.ListingRequests
                .Include(r => r.Listing) // Joins the listing details
                .Include(r => r.Requester) // Joins the user details
                .Where(r => r.OwnerId == currentUserId)
                .OrderByDescending(r => r.DateRequested)
                .ToList();

            // Fetch Outgoing Requests (Plants YOU asked for)
            homeViewModel.OutgoingRequests = _DB.ListingRequests
                .Include(r => r.Listing)
                .Where(r => r.RequesterId == currentUserId)
                .OrderByDescending(r => r.DateRequested)
                .ToList();
            return View(homeViewModel);
            
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetTasks(string start, string end)
        {
            DateTime startDate = new DateTime();
            DateTime endDate = new DateTime();

            if (!string.IsNullOrEmpty(start))
            {
                DateTime.TryParse(start, out startDate);
            }
            if (!string.IsNullOrEmpty(end))
            {
                DateTime.TryParse(end, out endDate);
            }

            var rawTasks = _DB.CalendarTasks
                .Where(x => x.AppUserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
                .ToList();

            var tasksInRange = rawTasks
                .Select(t => new
                {
                    id = t.Id,
                    title = t.TaskName,
                    start = t.TaskDate.ToString("yyyy-MM-dd"),
                    type = t.Category,
                    description = t.Description,
                    isCompleted = t.IsCompleted,
                    MyPlantId = t.MyPlantId,
                    MyPlant = t.MyPlant,
                    backgroundColor = t.IsCompleted ? "#cccccc" : "#28a745",
                    borderColor = t.IsCompleted ? "#aaaaaa" : "#1e7e34"
                })
                .ToList();

            return Json(tasksInRange);
        }

        
        [HttpPost]
        public IActionResult AddTask([FromBody] CalenderTask newTask)
        {
            // TRAP 1: Did the JSON Binder panic? 
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Binding Failed: " + errors });
            }

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Json(new { success = false, message = "You must be logged in to add a task." });
            }

            // Assign the user and fix the Postgres DateTime requirement
            newTask.AppUserId = currentUserId;

            newTask.TaskDate = DateTime.SpecifyKind(newTask.TaskDate, DateTimeKind.Utc);

            // The Safety Net: If the binder somehow forced a 0, turn it back into a true SQL null
            if (newTask.MyPlantId == 0)
            {
                newTask.MyPlantId = null;
            }

            try
            {
                _DB.CalendarTasks.Add(newTask);
                _DB.SaveChanges();
                return Json(new { success = true, message = "Task added successfully!" });
            }
            catch (Exception ex)
            {
                // TRAP 2: Extract the EXACT database rejection reason (InnerException)
                string dbError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Database Error: " + dbError });
            }
        }

        public class CompleteTaskRequest
        {
            public int Id { get; set; }
        }

        [Authorize]
        [HttpPost]
        public IActionResult CompleteTask([FromBody] CompleteTaskRequest request)
        {
            var task = _DB.CalendarTasks.FirstOrDefault(t => t.Id == request.Id);

            if (task == null)
            {
                return Json(new { success = false, message = "Task not found." });
            }

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (task.AppUserId != currentUserId)
            {
                return Json(new { success = false, message = "Unauthorized to update this task." });
            }

            task.IsCompleted = true;
            _DB.SaveChanges();

            return Json(new { success = true, message = "Task marked as completed!" });
        }

        public class DeleteTaskRequest
        {
            public int Id { get; set; }
        }

        [Authorize]
        [HttpPost]
        public IActionResult DeleteTask([FromBody] DeleteTaskRequest request)
        {
            var task = _DB.CalendarTasks.FirstOrDefault(t => t.Id == request.Id);

            if (task == null)
            {
                return Json(new { success = false, message = "Task not found." });
            }

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (task.AppUserId != currentUserId)
            {
                return Json(new { success = false, message = "Unauthorized to delete this task." });
            }

            _DB.CalendarTasks.Remove(task);
            _DB.SaveChanges();

            return Json(new { success = true, message = "Task deleted successfully!" });
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetLiveWidgetData(double? latitude, double? longitude)
        {
            string apiKey = _configuration["OpenWeatherApiKey"];
            string apiUrl = "";
            string locationName = "Unknown";

            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //var userInDb = await _userManager.FindByIdAsync(currentUserId);

            if (latitude.HasValue && longitude.HasValue)
            {
                apiUrl = $"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&units=metric&appid={apiKey}";
            }
            else
            {
                string fallbackCity = "Sahnewal";
                apiUrl = $"https://api.openweathermap.org/data/2.5/weather?q={fallbackCity}&units=metric&appid={apiKey}";
            }

            var response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                locationName = root.GetProperty("name").GetString() ?? "Unknown";
                double tempDouble = root.GetProperty("main").GetProperty("temp").GetDouble();
                int temp = (int)Math.Round(tempDouble);
                string condition = root.GetProperty("weather")[0].GetProperty("main").GetString();

                var data = new WeatherWidgetViewModel
                {
                    Location = locationName,
                    TemperatureCelsius = temp,
                    Condition = condition,
                    GrowthPotentialLevel = (temp > 18 && temp < 35) ? "High" : "Medium",
                    GrowthPercentage = (temp > 18 && temp < 35) ? 85 : 45
                };

                // --- SAVE DATA TO SESSION VARIABLES ---
                HttpContext.Session.SetString("UserCity", locationName);

                if (latitude.HasValue && longitude.HasValue)
                {
                    HttpContext.Session.SetString("UserLat", latitude.Value.ToString());
                    HttpContext.Session.SetString("UserLon", longitude.Value.ToString());
                }

                if (root.TryGetProperty("sys", out JsonElement sys) && sys.TryGetProperty("country", out JsonElement country))
                {
                    HttpContext.Session.SetString("UserCountry", country.GetString() ?? "Unknown");
                }

                // --- UPDATE PERSISTENT DATABASE USING SAFE UserManager ---
             

                return Json(data);
            }

            return Json(new WeatherWidgetViewModel { Location = "API Error", TemperatureCelsius = 0, Condition = "Unknown" });
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ImportPlantsFromApi([FromServices] LeafBy.Data.PerenualApiService perenualApi)
        {
            var newPlants = await perenualApi.FetchPlantsInBatchAsync();
            int addedCount = 0;

            foreach (var plant in newPlants)
            {
                bool plantExists = _DB.PlantCatalog.Any(p => p.CommonName == plant.CommonName);

                if (!plantExists)
                {
                    _DB.PlantCatalog.Add(plant);
                    addedCount++;
                }
            }

            await _DB.SaveChangesAsync();
            return Json(new { success = true, message = $"Successfully imported {addedCount} new plants to your catalog!" });
        }

        [Authorize]
        public IActionResult Calendar()
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int currentMonth = DateTime.Now.Month;
            int currentYear = DateTime.Now.Year;

            var dbTasks = _DB.CalendarTasks
                .Where(t => t.AppUserId == currentUserId &&
                            t.TaskDate.Month == currentMonth &&
                            t.TaskDate.Year == currentYear)
                .ToList();

            var jsTasks = dbTasks.Select(t => new CalendarTaskViewModel
            {
                Id = t.Id,
                Day = t.TaskDate.Day,
                Title = t.TaskName,
                Description = t.Description,
                Type = t.Category.ToLower(),
                IsCompleted = t.IsCompleted,
                Icon = (t.Category?.ToLower() ?? "") switch
                {
                    "water" => "bi-droplet",
                    "sow" => "bi-flower1",
                    "prune" => "bi-scissors",
                    "fertilize" => "bi-flask",
                    "repot" => "bi-box-seam",
                    _ => "bi-check2-circle"
                },
                Theme = (t.Category?.ToLower() ?? "") switch
                {
                    "water" => "theme-water",
                    "sow" => "theme-sow",
                    "prune" => "theme-prune",
                    "fertilize" => "theme-fert",
                    "repot" => "theme-repot",
                    _ => "bg-light text-dark"
                }
            }).ToList();

            return View(jsTasks);
        }



        public class UpdateRequestStatusModel { public int RequestId { get; set; } public string NewStatus { get; set; } }

        [HttpPost]
        public IActionResult UpdateRequestStatus([FromBody] UpdateRequestStatusModel data)
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var request = _DB.ListingRequests.FirstOrDefault(r => r.Id == data.RequestId);
            if (request == null) return Json(new { success = false, message = "Request not found." });

            // Security check: Only the OWNER of the listing can accept/reject the request
            if (request.OwnerId != currentUserId) return Json(new { success = false, message = "Unauthorized." });

            request.Status = data.NewStatus; // "Accepted" or "Rejected"
            _DB.SaveChanges();

            return Json(new { success = true, message = $"Request {data.NewStatus}!" });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}