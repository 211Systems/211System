using System.Globalization;
using _211system.Data;
using _211system.Models.Dtos.Nasa;
using _211system.Models.Interfaces;
using CPR112.Models;

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

                double latitude = double.Parse(columns[0], CultureInfo.InvariantCulture);
                double longitude = double.Parse(columns[1], CultureInfo.InvariantCulture);
                double brightness = double.Parse(columns[2], CultureInfo.InvariantCulture);

                var nasaPoint = new NasaFlarePoint
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Brightness = brightness,
                    DetectionDate = DateTime.UtcNow
                };

                await _context.NasaFlarePoints.AddAsync(nasaPoint);
                resultDto.TotalAnomaliesDetected++;

                if (brightness > 330)
                {
                    var location = new Location
                    {
                        Address = "WYKRYTO Z SATELITY",
                        City = "BRAK DANYCH",
                        Latitude = latitude,
                        Longitude = longitude
                    };

                    await _context.Locations.AddAsync(location);

                    var randomSuffix = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();

                    var incident = new Incident
                    {
                        IncidentNumber = $"NASA/{DateTime.UtcNow:yyyyMMdd}/{randomSuffix}",
                        Severity = "Krytyczny",
                        Description = $"ALARM SATELITARNY: System FIRMS wykrył anomalię termiczną o wysokiej jasności ({brightness} K).",
                        ReportDate = DateTime.UtcNow,
                        Status = "Nowe",
                        Location = location,
                        OperatorId = null
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
