using _211system.DTOs.Ai;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
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
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _apiKey = configuration["GeminiApiKey"] ?? throw new ArgumentNullException("Brak klucza GeminiApiKey w konfiguracji.");
        }

        private static bool IsTransientOverloadStatus(HttpStatusCode status)
        {
            // 429 TooManyRequests, 503 ServiceUnavailable, 504 GatewayTimeout, 529 (overload)
            int code = (int)status;
            return code == 429 || code == 503 || code == 504 || code == 529;
        }

        public async Task<List<AiDispatchSuggestion>> GetAutoDispatchPlanAsync(AiDispatchRequestDto requestData)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var stateJson = JsonSerializer.Serialize(requestData);

            var systemPrompt = @"
Jestes glownym dyspozytorem systemu ratowniczego. Twoim zadaniem jest przypisanie dostepnych jednostek do otwartych incydentow.

ZASADY DOBORU SLUZB:
1. Dopasuj typ jednostki do charakteru incydentu:
   - Pozary, anomalia termiczna NASA, zagrozenie budowlane -> Straz Pozarna (Fire) lub lotnicza (FireAir)
   - Przestepstwa, agresja, zagrozenie porzadku publicznego -> Policja (Police) lub lotnicza (PoliceAir)
   - Urazy, zaslabniercia, wypadki z rannymi -> Pogotowie (Medical) lub lotnicze LPR (MedicalAir)
   - Wypadek drogowy z rannymi -> Straz (Fire) + Pogotowie (Medical) + Policja (Police)
2. Jedna jednostka moze byc przypisana tylko do jednego incydentu.
3. Incydent moze wymagac kilku jednostek jezeli opis to uzasadnia.
4. Preferuj jednostki naziemne gdy odleglosc jest mala. Jednostki lotnicze stosuj dla dystansow > 15 km lub zdarzen krytycznych.

ZASADY POGODOWE (pole CurrentWeather w danych - jesli jest null, ignoruj ten blok):
5. Jezeli IsFlightRecommended = false (FlightRules = IFR lub LIFR) — NIE wysylaj zadnych jednostek lotniczych (MedicalAir, PoliceAir, FireAir). Zaproponuj jednostke naziemna i wyjasni decyzje w Reasoning.
6. Jezeli IsStormy = true — preferuj jednostki naziemne; lotnicze tylko gdy absolutnie brak alternatywy, i zaznacz ryzyko w Reasoning.
7. Jezeli IsFoggy = true lub VisibilityMeters < 1000 — ogranicz lotnictwo i zaznacz to w uzasadnieniu.
8. Jezeli IsSlippery = true — w Reasoning uwzglednij wydluzony czas dojazdu jednostek naziemnych.
9. Zawsze wspomnij aktualne warunki pogodowe w Reasoning gdy maja wplyw na decyzje.

ZWROC WYNIK WYLACZNIE W FORMACIE JSON (bez Markdowna, bez dodatkowego tekstu).
Oczekiwany format:
{
  ""Suggestions"": [
    {
      ""IncidentId"": ""guid"",
      ""UnitId"": ""guid"",
      ""UnitType"": ""Medical"" | ""Fire"" | ""Police"" | ""MedicalAir"" | ""PoliceAir"" | ""FireAir"",
      ""Reasoning"": ""Krotkie uzasadnienie po polsku""
    }
  ]
}

Aktualny stan systemu (incydenty, wolne jednostki i warunki meteorologiczne):
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

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync(url, content);
            }
            catch (TaskCanceledException tcex)
            {
                throw new AiServiceUnavailableException(
                    "Model AI nie odpowiedział w wymaganym czasie. Spróbuj ponownie za chwilę.",
                    tcex);
            }
            catch (HttpRequestException hex)
            {
                throw new AiServiceUnavailableException(
                    "Nie można połączyć się z modelem AI. Spróbuj ponownie za chwilę.",
                    hex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();

                if (IsTransientOverloadStatus(response.StatusCode))
                {
                    throw new AiServiceUnavailableException(
                        "Model AI Gemini jest aktualnie przeciążony. Spróbuj ponownie za chwilę.",
                        upstreamStatusCode: (int)response.StatusCode,
                        upstreamBody: errorBody);
                }

                throw new Exception($"Blad polaczenia z Gemini API (Kod {response.StatusCode}): {errorBody}");
            }

            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            var textContent = root
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(textContent))
                return new List<AiDispatchSuggestion>();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dispatchResponse = JsonSerializer.Deserialize<AiDispatchResponseDto>(textContent, options);

            return dispatchResponse?.Suggestions ?? new List<AiDispatchSuggestion>();
        }
    }
}