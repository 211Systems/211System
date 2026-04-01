using System.Globalization;
using _211system.Data;
using _211system.Models.Dtos.Nasa;
using _211system.Models.Interfaces;
using CPR112.Models;
using Microsoft.EntityFrameworkCore;

namespace _211system.Models.Services
{
    public class NasaService : INasaService
    {
        private readonly _211DbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration configuration;

        public NasaService(_211DbContext context, IHttpClientFactory httpClientFactory, IConfiguration iConfiguration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            configuration = iConfiguration;
        }

        public async Task<NasaFetchResultDto> FetchFireDataAndCreateIncidentsAsync()
        {
            var apiKey = configuration["NasaApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Brak klucza NASA API!");

            // Pobieramy pierwszą dostępną placówkę z tabeli Encs, aby do niej przypisać pożary z satelity
            var mainEnc = await _context.Encs.FirstOrDefaultAsync();
            if (mainEnc == null)
                throw new Exception("Błąd: System nie posiada żadnej placówki (Enc) w bazie danych. Nie można przypisać incydentu NASA.");

            string nasaUrl = $"https://firms.modaps.eosdis.nasa.gov/api/country/csv/{apiKey}/VIIRS_SNPP_NRT/POL/1";

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(nasaUrl);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Błąd połączenia z NASA. Status: {response.StatusCode}");

            var csvData = await response.Content.ReadAsStringAsync();
            var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var resultDto = new NasaFetchResultDto();

            foreach (var line in lines.Skip(1))
            {
                var columns = line.Split(',');
                if (columns.Length < 3) continue;

                if (!double.TryParse(columns[0], CultureInfo.InvariantCulture, out double latitude)) continue;
                if (!double.TryParse(columns[1], CultureInfo.InvariantCulture, out double longitude)) continue;
                if (!double.TryParse(columns[2], CultureInfo.InvariantCulture, out double brightness)) continue;

                var nasaPoint = new NasaFlarePoint
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Brightness = brightness,
                    DetectionDate = DateTime.UtcNow
                };

                await _context.NasaFlarePoints.AddAsync(nasaPoint);
                resultDto.TotalAnomaliesDetected++;

                // Jeśli jasność przekracza 330K, generujemy incydent
                if (brightness > 330)
                {
                    var randomSuffix = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();

                    var incident = new Incident
                    {
                        Id = Guid.NewGuid(),
                        IncidentNumber = $"NASA/{DateTime.UtcNow:yyyyMMdd}/{randomSuffix}",
                        Severity = "Krytyczny",
                        Description = $"ALARM SATELITARNY: Wykryto anomalię termiczną ({brightness} K). Współrzędne: {latitude}, {longitude}",
                        ReportDate = DateTime.UtcNow,
                        Status = "Nowe",
                        // PRZYPISANIE DO PLACÓWKI (ENC)
                        LocationId = mainEnc.Id,
                        Location = mainEnc, 
                        OperatorId = null,
                        PhotoUrl = null
                    };

                    await _context.Incidents.AddAsync(incident);

                    resultDto.IncidentsGenerated++;
                    resultDto.GeneratedIncidents.Add(new NasaIncidentDto
                    {
                        Latitude = latitude,
                        Longitude = longitude,
                        Brightness = brightness,
                        Description = incident.Description
                    });
                }
            }

            await _context.SaveChangesAsync();
            return resultDto;
        }
    }
}