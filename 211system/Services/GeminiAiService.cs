using _211system.DTOs.Ai;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace _211system.Services
{
    public class GeminiAiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiAiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiApiKey"] ?? throw new ArgumentNullException("Brak klucza GeminiApiKey w konfiguracji.");
        }

        public async Task<List<AiDispatchSuggestion>> GetAutoDispatchPlanAsync(AiDispatchRequestDto requestData)
        {
            // Bezpieczniejszy adres z dopiskiem -latest
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var stateJson = JsonSerializer.Serialize(requestData);

            var systemPrompt = @"
Jesteś głównym dyspozytorem systemu ratowniczego. Twoim zadaniem jest przypisanie dostępnych jednostek do otwartych incydentów.
Zasady:
1. Zawsze dopasuj typ jednostki do charakteru incydentu.
   - Pożary, wypadki drogowe, anomalia NASA -> Straż Pożarna (Fire)
   - Przestępstwa, agresja, zabezpieczenie -> Policja (Police)
   - Urazy, zasłabnięcia, ranni -> Pogotowie (Medical)
2. Jedna jednostka może zostać przypisana tylko do jednego incydentu.
3. Incydent może wymagać kilku jednostek (np. wypadek: Straż + Pogotowie + Policja), jeśli w opisie jest uzasadnienie.

ZWRÓĆ WYNIK WYŁĄCZNIE W FORMACIE JSON (bez Markdowna, bez dodatkowego tekstu).
Oczekiwany format:
{
  ""Suggestions"": [
    {
      ""IncidentId"": ""guid"",
      ""UnitId"": ""guid"",
      ""UnitType"": ""Medical"" | ""Fire"" | ""Police"",
      ""Reasoning"": ""Krótkie uzasadnienie po polsku""
    }
  ]
}

Aktualny stan systemu (incydenty i wolne jednostki):
" + stateJson;

            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = systemPrompt } } }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json"
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            // Szczegółowe przechwytywanie błędu API
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Błąd połączenia z Gemini API (Kod {response.StatusCode}): {errorBody}");
            }

            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            var textContent = root.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(textContent))
            {
                return new List<AiDispatchSuggestion>();
            }

            // Ignorowanie wielkości liter przy mapowaniu JSON na obiekty
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dispatchResponse = JsonSerializer.Deserialize<AiDispatchResponseDto>(textContent, options);

            return dispatchResponse?.Suggestions ?? new List<AiDispatchSuggestion>();
        }
    }
}