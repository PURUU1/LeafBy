using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeafBy.Controllers
{
    public class ChatBotController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private const string SessionKey = "BotConversationHistory";

        public ChatBotController(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _configuration = configuration;
        }

        public IActionResult Index() => View();

        public IActionResult ChatHistory()
        {
            string existingHistoryJson = HttpContext.Session.GetString(SessionKey);
            return Json(new { history = existingHistoryJson ?? "" });
        }

        [HttpPost]
        public async Task<IActionResult> AskBotanist([FromBody] ChatRequest request)
        {
            string apiKey = _configuration["NvidiaApiKey"];
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return Json(new { answer = "Please ask a valid question." });

            // 1. Retrieve history
            List<NvidiaMessage> history = new List<NvidiaMessage>();
            string existingHistoryJson = HttpContext.Session.GetString(SessionKey);

            if (!string.IsNullOrEmpty(existingHistoryJson))
            {
                history = JsonSerializer.Deserialize<List<NvidiaMessage>>(existingHistoryJson) ?? new List<NvidiaMessage>();
            }

            // 2. Add System Instruction (Only if fresh chat)
            if (!history.Any(m => m.Role == "system"))
            {
                history.Insert(0, new NvidiaMessage
                {
                    Role = "system",
                    Content = "You are Leafy, an expert AI botanist . Answer questions about plants, gardening, pests, and soil. Provide advice relevant to the local North Indian seasonal conditions. Be concise, friendly, and plain-formatted."
                });
            }

            // 3. Add User Prompt
            history.Add(new NvidiaMessage { Role = "user", Content = request.Prompt });

            // 4. Prepare Payload for NVIDIA NIM
            var payload = new
            {
                model = "meta/llama-3.1-70b-instruct",
                messages = history,
                temperature = 0.5,
                max_tokens = 1024
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // 5. Execute API Call
            string apiUrl = "https://integrate.api.nvidia.com/v1/chat/completions";
            var response = await _httpClient.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);

                var aiText = doc.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content").GetString();

                // 6. Update history
                history.Add(new NvidiaMessage { Role = "assistant", Content = aiText ?? "" });
                HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(history));

                return Json(new { success = true, answer = aiText });
            }
            else
            {
                return Json(new { success = false, answer = "Leafy is currently unreachable. Please try again later." });
            }
        }
    }

    // --- SUPPORTING MODELS ---
    public class ChatRequest
    {
        public string Prompt { get; set; }
    }

    public class NvidiaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } // system, user, assistant

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}