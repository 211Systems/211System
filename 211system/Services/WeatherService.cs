using System.Text.Json;
using _211system.Models.Dtos;
using _211system.Models.Interfaces;
using Microsoft.Extensions.Configuration;

namespace _211system.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _owmKey;
        private readonly string _avwxKey;

        public WeatherService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _owmKey = configuration["WeatherApis:OpenWeatherMapKey"];
            _avwxKey = configuration["WeatherApis:AvwxKey"];
        }

        public async Task<GroundWeatherDto> GetGroundConditionsAsync(double lat, double lng)
        {
            var client = _httpClientFactory.CreateClient();

            var latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lngStr = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var url = $"https://api.openweathermap.org/data/2.5/weather?lat={latStr}&lon={lngStr}&appid={_owmKey}&units=metric&lang=pl";

            try
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) throw new Exception("Błąd API OpenWeatherMap");

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var weatherNode = root.GetProperty("weather")[0];
                var mainNode = root.GetProperty("main");

                var temp = mainNode.GetProperty("temp").GetDouble();
                var desc = weatherNode.GetProperty("description").GetString();
                var icon = weatherNode.GetProperty("icon").GetString();
                var visibility = root.TryGetProperty("visibility", out var visProp) ? visProp.GetInt32() : 10000;

                var isSlippery = temp <= 2.5 && (desc.Contains("deszcz") || desc.Contains("śnieg") || desc.Contains("mżawka"));
                var isFoggy = visibility <= 1000 || desc.Contains("mgła");
                var isStormy = desc.Contains("burza");

                return new GroundWeatherDto
                {
                    Temperature = Math.Round(temp, 1),
                    Description = char.ToUpper(desc[0]) + desc.Substring(1),
                    VisibilityMeters = visibility,
                    IconUrl = $"https://openweathermap.org/img/wn/{icon}.png",
                    IsSlippery = isSlippery,
                    IsFoggy = isFoggy,
                    IsStormy = isStormy
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WeatherService OWM] Błąd: {ex.Message}");
                return new GroundWeatherDto { Description = "Brak danych", IconUrl = "", Temperature = 0 };
            }
        }

        public async Task<FlightWeatherDto> GetFlightConditionsAsync(double lat, double lng)
        {
            var client = _httpClientFactory.CreateClient();

            var latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lngStr = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var url = $"https://avwx.rest/api/metar/{latStr},{lngStr}?token={_avwxKey}";

            try
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var errBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[WeatherService AVWX] Status: {response.StatusCode}, Body: {errBody}");
                    return new FlightWeatherDto { FlightRules = "B/D", RawMetar = "Błąd pobierania." };
                }

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var flightRules = root.TryGetProperty("flight_rules", out var frProp) ? frProp.GetString() : "B/D";
                var station = root.TryGetProperty("station", out var stProp) ? stProp.GetString() : "Brak";
                var rawMetar = root.TryGetProperty("raw", out var rawProp) ? rawProp.GetString() : "";

                return new FlightWeatherDto
                {
                    StationIcao = station ?? "Brak",
                    FlightRules = flightRules ?? "B/D",
                    RawMetar = rawMetar ?? ""
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WeatherService AVWX] Błąd: {ex.Message}");
                return new FlightWeatherDto { FlightRules = "B/D", RawMetar = "Brak danych." };
            }
        }
    }
}