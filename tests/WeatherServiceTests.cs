using System.Net;
using System.Threading;
using System.Threading.Tasks;
using _211system.Models.Interfaces;
using _211system.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace _211system.Tests
{
    public class WeatherServiceTests
    {
        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

            public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_handler(request));
            }
        }

        private class FakeHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;
            public FakeHttpClientFactory(HttpClient client) => _client = client;
            public HttpClient CreateClient(string name) => _client;
        }

        private static IConfiguration GetConfig()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "WeatherApis:OpenWeatherMapKey", "TEST_OWM_KEY" },
                    { "WeatherApis:AvwxKey", "TEST_AVWX_KEY" }
                })
                .Build();
        }

        private static WeatherService CreateService(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler);
            return new WeatherService(new FakeHttpClientFactory(client), GetConfig());
        }

        [Fact]
        public async Task GetGroundConditionsAsync_ParseSuccessfulResponse()
        {
            string owmJson = """
            {
              "weather": [{ "description": "deszcz", "icon": "10d" }],
              "main": { "temp": 1.5 },
              "visibility": 500
            }
            """;

            var handler = new FakeHttpMessageHandler(req =>
            {
                if (req.RequestUri!.ToString().Contains("openweathermap"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(owmJson)
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var service = CreateService(handler);

            var result = await service.GetGroundConditionsAsync(52.23, 21.01);

            Assert.Equal(1.5, result.Temperature);
            Assert.Equal(500, result.VisibilityMeters);
            Assert.True(result.IsSlippery);
            Assert.True(result.IsFoggy);
            Assert.Contains("openweathermap.org/img/wn/", result.IconUrl);
        }

        [Fact]
        public async Task GetGroundConditionsAsync_ApiError_ThrowsOrReturnsDefault()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.Unauthorized));

            var service = CreateService(handler);

            var result = await service.GetGroundConditionsAsync(52.0, 21.0);

            Assert.Equal("Brak danych", result.Description);
            Assert.Equal(0, result.Temperature);
            Assert.Equal("", result.IconUrl);
        }

        [Fact]
        public async Task GetFlightConditionsAsync_ParseSuccessfulResponse()
        {
            string avwxJson = """
            {
              "flight_rules": "VFR",
              "station": "EPWA"
            }
            """;

            var handler = new FakeHttpMessageHandler(req =>
            {
                if (req.RequestUri!.ToString().Contains("avwx.rest"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(avwxJson)
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var service = CreateService(handler);

            var result = await service.GetFlightConditionsAsync(52.17, 20.97);

            Assert.Equal("VFR", result.FlightRules);
            Assert.Equal("EPWA", result.StationIcao);
        }

        [Fact]
        public async Task GetFlightConditionsAsync_InvalidCoords_Handles()
        {
            var handler = new FakeHttpMessageHandler(req =>
            {
                if (req.RequestUri!.ToString().Contains("avwx.rest"))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("{\"error\":\"invalid coords\"}")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var service = CreateService(handler);

            var result = await service.GetFlightConditionsAsync(999.0, 999.0);

            Assert.Equal("B/D", result.FlightRules);
            Assert.Equal("Błąd pobierania.", result.RawMetar);
        }
    }
}