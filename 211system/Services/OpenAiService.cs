using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using _211system.Models.Interfaces;

namespace _211system.Services;

public class OpenAiService : IOpenAiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    private const string SystemPrompt = "Jesteś asystentem dyspozytora 112. Użytkownik poda Ci krótki opis wypadku. Zwróć dokładnie 3 krótkie, wypunktowane instrukcje pierwszej pomocy lub procedury bezpieczeństwa.";

    public OpenAiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string> GetAdviceAsync(string incidentDescription)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Brak klucza API OpenAI w konfiguracji (User Secrets).");
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = incidentDescription }
            },
            temperature = 0.2
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Błąd komunikacji z API OpenAI. Status: {response.StatusCode}. Szczegóły: {errorContent}");
        }

        var responseString = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseString);

        var advice = jsonDoc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return advice ?? "Otrzymano pustą odpowiedź z modelu.";
    }
}