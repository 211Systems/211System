using System;

namespace _211system.Services
{

    public class AiServiceUnavailableException : Exception
    {
        public int? UpstreamStatusCode { get; }
        public string? UpstreamBody { get; }

        public AiServiceUnavailableException(string message, int? upstreamStatusCode = null, string? upstreamBody = null)
            : base(message)
        {
            UpstreamStatusCode = upstreamStatusCode;
            UpstreamBody = upstreamBody;
        }

        public AiServiceUnavailableException(string message, Exception innerException, int? upstreamStatusCode = null, string? upstreamBody = null)
            : base(message, innerException)
        {
            UpstreamStatusCode = upstreamStatusCode;
            UpstreamBody = upstreamBody;
        }
    }
}
