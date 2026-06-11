using System.Net.Http.Headers;
using System.Text.Json;

public class NvidiaNimService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ApiUrl = "https://integrate.api.nvidia.com/v1/chat/completions";

    public NvidiaNimService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["NvidiaApiKey"];
    }

    public async Task<string> GetBotanicalAdvice(string userPrompt, string chatHistoryJson)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var payload = new
        {
            model = "meta/llama-3.1-70b-instruct", // High-performance model
            messages = new[]
            {
                new { role = "system", content = "You are Leafy, an expert botanist specialized in the Sahnewal, Punjab climate. You provide advice based on local soil conditions and seasonal changes in North India. If a user asks about garden planning, suggest crops appropriate for the current month in Punjab." },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.5,
            max_tokens = 1024
        };

        var response = await _httpClient.PostAsJsonAsync(ApiUrl, payload);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        return result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
    }
}