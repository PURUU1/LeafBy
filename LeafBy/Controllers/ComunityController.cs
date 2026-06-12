using LeafBy.Data;
using LeafBy.Hubs;
using LeafBy.Models;
using LeafBy.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LeafBy.Controllers
{
    [Authorize] // Ensure only logged-in users access the community
    public class ComunityController : Controller
    {
        private readonly ILogger<ComunityController> _logger;
        private readonly ApplicationDbContext _DB;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IHubContext<ChatHub> _hubContext; 


        public ComunityController(ILogger<ComunityController> logger, ApplicationDbContext db, IConfiguration configuration, IHubContext<ChatHub> hubContext)
        {
            _logger = logger;
            _DB = db;
            _configuration = configuration;
            _httpClient = new HttpClient();
            _hubContext = hubContext;
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

            foreach (var loc in otherPeoplesListings)
            {
                if (double.TryParse(loc.Latitude, out double itemLat) && double.TryParse(loc.Longitude, out double itemLon))
                {
                    // Fixed the bug where userLat was passed twice
                    loc.DistanceMiles = GetDistanceInKm(userLat, userLon, itemLat, itemLon);
                }
                else
                {
                    loc.DistanceMiles = 0;
                }
            }

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

        // --- CRUD ENDPOINTS FOR LISTINGS (Triggered via AJAX Modals) ---

        [HttpPost]
        public IActionResult CreateListing([FromBody] ComunityListing newListing)
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Json(new { success = false, message = "Unauthorized" });

            var existingUser = _DB.Users.OfType<Appuser>().FirstOrDefault(x => x.Id == currentUserId);

            newListing.AppUserId = currentUserId;
            newListing.OwnerName = newListing.OwnerName;
            newListing.Latitude = HttpContext.Session.GetString("UserLat") ?? existingUser?.lat;
            newListing.Longitude = HttpContext.Session.GetString("UserLon") ?? existingUser?.lon;
            newListing.DateCreated = DateTime.UtcNow;


            _DB.CommunityListings.Add(newListing);
            _DB.SaveChanges();

            return Json(new { success = true, message = "Listing posted successfully!" });
        }

        [HttpPost]
        public IActionResult EditListing([FromBody] ComunityListing updatedListing)
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find the listing in the database
            var existingListing = _DB.CommunityListings.FirstOrDefault(l => l.Id == updatedListing.Id);

            // Security check: Only the owner can edit their own listing
            if (existingListing == null || existingListing.AppUserId != currentUserId)
            {
                return Json(new { success = false, message = "Unauthorized or listing not found." });
            }

            // Update the allowed fields
            existingListing.Title = updatedListing.Title;
            existingListing.Category = updatedListing.Category;
            existingListing.PlantType = updatedListing.PlantType;
            existingListing.ListingType = updatedListing.ListingType;
            existingListing.Description = updatedListing.Description;
            existingListing.ImageUrl = updatedListing.ImageUrl;

            _DB.SaveChanges();
            return Json(new { success = true, message = "Listing updated successfully!" });
        }
        public class DeleteListingRequest { public int Id { get; set; } }
        [HttpPost]
        [HttpPost]
        public IActionResult DeleteListing([FromBody] DeleteListingRequest request)
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Fetch the listing
            var existingListing = _DB.CommunityListings.FirstOrDefault(l => l.Id == request.Id);

            if (existingListing == null || existingListing.AppUserId != currentUserId)
            {
                return Json(new { success = false, message = "Unauthorized or listing not found." });
            }

            try
            {
                // 2. Find and remove all dependent records in the ListingRequests table
                // We filter by the ListingId (or however your foreign key is named)
                var dependentRequests = _DB.ListingRequests.Where(r => r.ComunityListingId == request.Id);

                if (dependentRequests.Any())
                {
                    _DB.ListingRequests.RemoveRange(dependentRequests);
                }

                // 3. Now it is safe to remove the parent record
                _DB.CommunityListings.Remove(existingListing);

                // 4. Save all changes in one transaction
                _DB.SaveChanges();

                return Json(new { success = true, message = "Listing and all associated requests deleted." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Database Error: " + ex.Message });
            }
        }

        // --- DISTANCE CALCULATION HELPER ---

        private const double EarthRadiusKm = 6371.0;

        public static double GetDistanceInKm(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return Math.Round(EarthRadiusKm * c, 1);
        }

        private static double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }



        [HttpPost]
        public async Task<IActionResult> SubmitRequest([FromBody] ListingRequest requestData)
        {
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string currentUsername = User.Identity.Name; // Get the sender's username for the chat

            if (string.IsNullOrEmpty(currentUserId)) return Json(new { success = false, message = "Unauthorized" });

            // Verify the listing exists
            var targetListing = _DB.CommunityListings.FirstOrDefault(l => l.Id == requestData.ComunityListingId);
            if (targetListing == null) return Json(new { success = false, message = "Listing not found." });

            // Prevent users from requesting their own listings
            if (targetListing.AppUserId == currentUserId) return Json(new { success = false, message = "You cannot request your own item." });

            var timestamp = DateTime.UtcNow;

            var newRequest = new ListingRequest
            {
                ComunityListingId = targetListing.Id,
                RequesterId = currentUserId,
                OwnerId = targetListing.AppUserId,
                Message = requestData.Message,
                Status = "Pending",
                DateRequested = timestamp
            };
            _DB.ListingRequests.Add(newRequest);
            await _DB.SaveChangesAsync(); // This generates newRequest.Id!

            // 2. Build the special formatted Chat Message string
            // Format: |TRADE_REQ|RequestId|ListingTitle|ImageUrl|UserMessage
            string specialPayload = $"|TRADE_REQ|{newRequest.Id}|{targetListing.Title}|{targetListing.ImageUrl}|{requestData.Message}";

            var chatMessage = new DirectMessage
            {
                SenderUsername = currentUsername,
                ReceiverUsername = targetListing.OwnerName,
                Message = specialPayload,
                Timestamp = timestamp
            };
            _DB.DirectMessages.Add(chatMessage);
            await _DB.SaveChangesAsync();

            // 3. Fire the Real-Time SignalR Notification
            string timeFormatted = timestamp.ToString("h:mm tt");
            await _hubContext.Clients.User(targetListing.AppUserId)
                .SendAsync("ReceiveDirectMessage", currentUsername, chatMessage.Message, timeFormatted);

            return Json(new { success = true, message = "Request sent to the owner!" });
        }
        [HttpGet]
        public IActionResult DirectChat(string partnerUsername)
        {
            if (string.IsNullOrEmpty(partnerUsername))
            {
                return RedirectToAction("Index");
            }

            var currentUser = User.Identity.Name;

            // Load the 1-on-1 history between the logged-in user and their trading partner
            var chatHistory = _DB.DirectMessages
                .Where(m =>
                    (m.SenderUsername == currentUser && m.ReceiverUsername == partnerUsername) ||
                    (m.SenderUsername == partnerUsername && m.ReceiverUsername == currentUser))
                .OrderBy(m => m.Timestamp)
                .ToList();

            ViewBag.PartnerUsername = partnerUsername;

            return View(chatHistory);
        }






        // Make sure to add this at the top if you don't have it:
        // using LeafBy.Models;

        [HttpGet]
        [Authorize]
        public IActionResult Inbox()
        {
            var currentUser = User.Identity.Name;

            // 1. Get every message involving the current user, ordered from newest to oldest
            var allMyMessages = _DB.DirectMessages
                .Where(m => m.SenderUsername == currentUser || m.ReceiverUsername == currentUser)
                .OrderByDescending(m => m.Timestamp)
                .ToList(); // Bringing it into memory makes the grouping easier

            // 2. Group the messages by the "Partner" (the person who isn't the current user)
            var inboxItems = allMyMessages
                .GroupBy(m => m.SenderUsername == currentUser ? m.ReceiverUsername : m.SenderUsername)
                .Select(g => new ChatInboxViewModel
                {
                    PartnerUsername = g.Key,
                    LastMessage = g.First().Message, // Because we ordered by descending above, First() is the newest message
                    LastMessageTime = g.First().Timestamp
                })
                .ToList();

            return View(inboxItems);
        }
    }
}