using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using _211system.DTOs.Ai;
using _211system.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace _211system.Tests
{
    public class GeminiAiServiceTests
    {
        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _response;
            private readonly HttpStatusCode _status;

            public FakeHttpMessageHandler(string response, HttpStatusCode status = HttpStatusCode.OK)
            {
                _response = response;
                _status = status;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = _status,
                    Content = new StringContent(_response)
                });
            }
        }

        private static GeminiAiService CreateService(HttpMessageHandler handler)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "GeminiApiKey", "TEST_GEMINI" } })
                .Build();

            return new GeminiAiService(new HttpClient(handler), config);
        }

        private static string WrapGeminiText(string innerJson)
        {
            var wrapper = new
            {
                candidates = new[]
                {
                    new
                    {
                        content = new
                        {
                            parts = new[] { new { text = innerJson } }
                        }
                    }
                }
            };
            return JsonSerializer.Serialize(wrapper);
        }

        [Fact]
        public async Task GetAutoDispatchPlanAsync_WithValidGeminiResponse_ReturnsSuggestions()
        {
            var incidentId = Guid.NewGuid();
            var unitId = Guid.NewGuid();

            string inner = JsonSerializer.Serialize(new AiDispatchResponseDto
            {
                Suggestions = new List<AiDispatchSuggestion>
                {
                    new AiDispatchSuggestion
                    {
                        IncidentId = incidentId,
                        UnitId = unitId,
                        UnitType = "Medical",
                        Reasoning = "Karetka najblizej"
                    }
                }
            });

            var service = CreateService(new FakeHttpMessageHandler(WrapGeminiText(inner)));

            var result = await service.GetAutoDispatchPlanAsync(new AiDispatchRequestDto
            {
                Incidents = new List<AiIncidentDto>
                {
                    new AiIncidentDto { Id = incidentId, Description = "Wypadek", Severity = "Wysoki", IncidentType = "Wypadek", Latitude = 52.0, Longitude = 21.0 }
                }
            });

            Assert.Single(result);
            Assert.Equal(unitId, result[0].UnitId);
            Assert.Equal("Medical", result[0].UnitType);
        }

        [Fact]
        public async Task GetAutoDispatchPlanAsync_WhenOverload_ThrowsAiServiceUnavailable()
        {
            var service = CreateService(new FakeHttpMessageHandler("{}", HttpStatusCode.ServiceUnavailable));

            await Assert.ThrowsAsync<AiServiceUnavailableException>(() =>
                service.GetAutoDispatchPlanAsync(new AiDispatchRequestDto()));
        }

        [Fact]
        public async Task GetAutoDispatchPlanAsync_InvalidJson_Throws()
        {
            var service = CreateService(new FakeHttpMessageHandler(WrapGeminiText("to nie jest json")));

            await Assert.ThrowsAsync<JsonException>(() =>
                service.GetAutoDispatchPlanAsync(new AiDispatchRequestDto()));
        }
    }
}
