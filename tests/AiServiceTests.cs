using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using _211system.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace _211system.Tests
{
    public class AiServiceTests
    {
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
        public async Task GetAdviceAsync_WithValidResponse_ShouldReturnExtractedAdvice()
        {
            var configParams = new Dictionary<string, string> { { "OpenAI:ApiKey", "TEST_KEY_123" } };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configParams).Build();

            var openAiResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = "- Zabezpiecz miejsce zdarzenia.\n- Sprawdź oddech poszkodowanego.\n- Wezwij pogotowie ratunkowe."
                        }
                    }
                }
            };
            var jsonResponse = JsonSerializer.Serialize(openAiResponse);

            var fakeHandler = new FakeHttpMessageHandler(jsonResponse);
            var fakeClientFactory = new FakeHttpClientFactory(new HttpClient(fakeHandler));

            var service = new OpenAiService(fakeClientFactory, config);

            var result = await service.GetAdviceAsync("Wypadek drogowy");

            Assert.NotNull(result);
            Assert.Contains("Zabezpiecz miejsce zdarzenia", result);
            Assert.Contains("Wezwij pogotowie", result);
        }

        [Fact]
        public async Task GetAdviceAsync_WithoutApiKey_ShouldThrowInvalidOperationException()
        {
            var emptyConfig = new ConfigurationBuilder().Build();
            var fakeClientFactory = new FakeHttpClientFactory(new HttpClient());
            var service = new OpenAiService(fakeClientFactory, emptyConfig);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAdviceAsync("Wypadek"));
            Assert.Equal("Brak klucza API OpenAI w konfiguracji (User Secrets).", exception.Message);
        }
    }
}