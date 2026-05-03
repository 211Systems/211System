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

        public async Task<NasaFetchResultDto> FetchFireDataAndCreateIncidentsAsync(bool isDemo = false)
        {
            var mainEnc = await _context.Encs.FirstOrDefaultAsync();
            if (mainEnc == null)
                throw new Exception("Błąd: System nie posiada żadnej placówki (Enc) w bazie danych.");

            var resultDto = new NasaFetchResultDto();

            var rawAnomalies = new List<(double Lat, double Lon, double Brightness)>();

            if (isDemo)
            {
                rawAnomalies.Add((53.1325, 23.1688, 345.5));
                rawAnomalies.Add((52.2297, 21.0122, 355.2));
                rawAnomalies.Add((50.0647, 19.9450, 315.0));
            }
            else
            {
                var apiKey = configuration["NasaApiKey"];
                if (string.IsNullOrEmpty(apiKey)) throw new Exception("Brak klucza NASA API!");

                string nasaUrl = $"https://firms.modaps.eosdis.nasa.gov/api/area/csv/{apiKey}/VIIRS_SNPP_NRT/14.0,49.0,24.1,55.0/5";
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(nasaUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Odrzucono przez NASA (Status: {response.StatusCode}). Detale: {errorBody}");
                }

                var csvData = await response.Content.ReadAsStringAsync();
                var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines.Skip(1))
                {
                    var columns = line.Split(',');
                    if (columns.Length < 3) continue;

                    if (double.TryParse(columns[0], CultureInfo.InvariantCulture, out double lat) &&
                        double.TryParse(columns[1], CultureInfo.InvariantCulture, out double lon) &&
                        double.TryParse(columns[2], CultureInfo.InvariantCulture, out double bright))
                    {
                        rawAnomalies.Add((lat, lon, bright));
                    }
                }
            }

            foreach (var anomaly in rawAnomalies)
            {
                var nasaPoint = new NasaFlarePoint
                {
                    Latitude = anomaly.Lat,
                    Longitude = anomaly.Lon,
                    Brightness = anomaly.Brightness,
                    DetectionDate = DateTime.UtcNow
                };

                await _context.NasaFlarePoints.AddAsync(nasaPoint);
                resultDto.TotalAnomaliesDetected++;

                if (anomaly.Brightness > 330)
                {
                    var randomSuffix = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                    var incident = new Incident
                    {
                        Id = Guid.NewGuid(),
                        IncidentNumber = $"NASA/{DateTime.UtcNow:yyyyMMdd}/{randomSuffix}",
                        SeverityLevelId = 3,
                        IncidentTypeId = 2,
                        Description = $"ALARM SATELITARNY: Wykryto anomalię termiczną ({anomaly.Brightness} K). Współrzędne: {anomaly.Lat}, {anomaly.Lon}",
                        ReportDate = DateTime.UtcNow,
                        Status = "Nowe",
                        LocationId = mainEnc.Id,
                        Location = mainEnc,
                        OperatorId = null,
                        PhotoUrl = null
                    };

                    await _context.Incidents.AddAsync(incident);
                    resultDto.IncidentsGenerated++;
                    resultDto.GeneratedIncidents.Add(new NasaIncidentDto
                    {
                        Latitude = anomaly.Lat,
                        Longitude = anomaly.Lon,
                        Brightness = anomaly.Brightness,
                        Description = incident.Description
                    });
                }
            }

            await _context.SaveChangesAsync();
            return resultDto;
        }
    }
}