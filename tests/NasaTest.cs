using System.Net;
using _211system.Data;
using _211system.Models;
using _211system.Models.Services;
using CPR112.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace _211system.Tests;

public class NasaServiceTests
{
    private async Task<_211DbContext> GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<_211DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var databaseContext = new _211DbContext(options);
        databaseContext.Database.EnsureCreated();
        return databaseContext;
    }

    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _fakeResponse;
        private readonly HttpStatusCode _statusCode;

        public FakeHttpMessageHandler(string fakeResponse, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _fakeResponse = fakeResponse;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content = new StringContent(_fakeResponse)
            });
        }
    }

    public class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public FakeHttpClientFactory(HttpClient client) { _client = client; }
        public HttpClient CreateClient(string name) => _client;
    }

    [Fact]
    public async Task FetchFireData_WithoutApiKey_ShouldThrowException()
    {
        var dbContext = await GetDatabaseContext();

        dbContext.Encs.Add(new CPR112.Models.Enc
        {
            Id = Guid.NewGuid(),
            Name = "Testowe Centrum CPR",
            Region = "Mazowieckie"
        });
        await dbContext.SaveChangesAsync();

        var emptyConfig = new ConfigurationBuilder().Build();
        var fakeClientFactory = new FakeHttpClientFactory(new HttpClient());

        var nasaService = new NasaService(dbContext, fakeClientFactory, emptyConfig);

        var exception = await Assert.ThrowsAsync<Exception>(() => nasaService.FetchFireDataAndCreateIncidentsAsync());

        Assert.Contains("Brak klucza NASA API", exception.Message);
    }

    [Fact]
    public async Task FetchFireData_WithValidCsv_ShouldParseAndCreateIncidents()
    {
        var dbContext = await GetDatabaseContext();

        var testCenter = new Enc
        {
            Id = Guid.NewGuid(),
            Name = "Testowe CPR",
            Region = "Podlaskie",
            Latitude = 53.13,
            Longitude = 23.16
        };
        await dbContext.Encs.AddAsync(testCenter);
        await dbContext.SaveChangesAsync();

        var configParams = new Dictionary<string, string> { { "NasaApiKey", "TEST_KEY_123" } };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configParams).Build();

        string fakeCsv = "latitude,longitude,brightness,scan,track,acq_date,acq_time,satellite,instrument,confidence,version,daynight\n" +
                         "52.22,21.01,310.5,1.0,1.0,2026-03-11,1200,N,VIIRS,n,1,D\n" +
                         "53.13,23.16,350.5,1.0,1.0,2026-03-11,1300,N,VIIRS,h,1,D\n";

        var fakeHandler = new FakeHttpMessageHandler(fakeCsv);
        var fakeClientFactory = new FakeHttpClientFactory(new HttpClient(fakeHandler));

        var nasaService = new NasaService(dbContext, fakeClientFactory, config);

        await nasaService.FetchFireDataAndCreateIncidentsAsync();

        var nasaPoints = await dbContext.NasaFlarePoints.ToListAsync();
        Assert.Equal(2, nasaPoints.Count);

        var incidents = await dbContext.Incidents.ToListAsync();
        Assert.Single(incidents);

        var generatedIncident = incidents.First();

        Assert.StartsWith("ALARM SATELITARNY", generatedIncident.Description);

        Assert.Equal(53.13, generatedIncident.Latitude);
        Assert.Equal(23.16, generatedIncident.Longitude);

        Assert.Null(generatedIncident.OperatorId);

        Assert.False(generatedIncident.IsPoliceActive);
        Assert.False(generatedIncident.IsFireActive);
        Assert.False(generatedIncident.IsMedicalActive);
    }

}