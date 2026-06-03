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
        await databaseContext.Database.EnsureCreatedAsync();
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

        dbContext.Encs.Add(new Enc
        {
            Id = Guid.NewGuid(),
            Name = "Testowe Centrum CPR",
            Region = "Mazowieckie"
        });
        await dbContext.SaveChangesAsync();

        var emptyConfig = new ConfigurationBuilder().Build();
        var nasaService = new NasaService(dbContext, new FakeHttpClientFactory(new HttpClient()), emptyConfig);

        var exception = await Assert.ThrowsAsync<Exception>(() => nasaService.FetchFireDataAndCreateIncidentsAsync());

        Assert.Contains("Brak klucza NASA API", exception.Message);
    }

    [Fact]
    public async Task FetchFireData_WithValidCsv_ShouldParseAndCreateIncidents()
    {
        var dbContext = await GetDatabaseContext();

        await dbContext.Encs.AddAsync(new Enc
        {
            Id = Guid.NewGuid(),
            Name = "Testowe CPR",
            Region = "Podlaskie",
            Latitude = 53.13,
            Longitude = 23.16
        });
        await dbContext.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "NasaApiKey", "TEST_KEY_123" } })
            .Build();

        string fakeCsv = "latitude,longitude,brightness,scan,track,acq_date,acq_time,satellite,instrument,confidence,version,daynight\n" +
                         "52.22,21.01,310.5,1.0,1.0,2026-03-11,1200,N,VIIRS,n,1,D\n" +
                         "53.13,23.16,350.5,1.0,1.0,2026-03-11,1300,N,VIIRS,h,1,D\n";

        var nasaService = new NasaService(
            dbContext,
            new FakeHttpClientFactory(new HttpClient(new FakeHttpMessageHandler(fakeCsv))),
            config);

        await nasaService.FetchFireDataAndCreateIncidentsAsync();

        Assert.Equal(2, await dbContext.NasaFlarePoints.CountAsync());

        var incidents = await dbContext.Incidents.ToListAsync();
        Assert.Single(incidents);
        Assert.StartsWith("ALARM SATELITARNY", incidents.First().Description);
        Assert.Equal(53.13, incidents.First().Latitude);
        Assert.Null(incidents.First().OperatorId);
    }

    [Fact]
    public async Task FetchFireData_DemoMode_CreatesIncidents()
    {
        var dbContext = await GetDatabaseContext();
        var config = new ConfigurationBuilder().Build();
        var nasaService = new NasaService(dbContext, new FakeHttpClientFactory(new HttpClient()), config);

        var result = await nasaService.FetchFireDataAndCreateIncidentsAsync(isDemo: true);

        Assert.Equal(3, result.TotalAnomaliesDetected);
        Assert.Equal(2, result.IncidentsGenerated);
        Assert.Equal(2, await dbContext.Incidents.CountAsync());
        Assert.Equal(3, await dbContext.NasaFlarePoints.CountAsync());
        Assert.All(result.GeneratedIncidents, i => Assert.Contains("ALARM SATELITARNY", i.Description));
    }

    [Fact]
    public async Task FetchFireData_WhenApiFails_ReturnsErrorResult()
    {
        var dbContext = await GetDatabaseContext();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "NasaApiKey", "BAD_KEY" } })
            .Build();

        var handler = new FakeHttpMessageHandler("error", HttpStatusCode.Unauthorized);
        var nasaService = new NasaService(dbContext, new FakeHttpClientFactory(new HttpClient(handler)), config);

        var ex = await Assert.ThrowsAsync<Exception>(() => nasaService.FetchFireDataAndCreateIncidentsAsync());

        Assert.Contains("Odrzucono przez NASA", ex.Message);
        Assert.Empty(await dbContext.Incidents.ToListAsync());
    }

    [Fact]
    public async Task FetchFireData_DuplicateCoords_SkipsOrUpdates()
    {
        var dbContext = await GetDatabaseContext();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "NasaApiKey", "KEY" } })
            .Build();

        string fakeCsv = "latitude,longitude,brightness\n" +
                         "52.10,21.00,340.0\n" +
                         "52.10,21.00,340.0\n";

        var nasaService = new NasaService(
            dbContext,
            new FakeHttpClientFactory(new HttpClient(new FakeHttpMessageHandler(fakeCsv))),
            config);

        var result = await nasaService.FetchFireDataAndCreateIncidentsAsync();

        Assert.Equal(2, result.TotalAnomaliesDetected);
        Assert.Equal(2, result.IncidentsGenerated);
        Assert.Equal(2, await dbContext.Incidents.CountAsync());
        Assert.Equal(2, await dbContext.NasaFlarePoints.CountAsync());
    }
}