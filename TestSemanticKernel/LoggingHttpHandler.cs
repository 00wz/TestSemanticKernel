using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TestSemanticKernel
{
    // Delegating handler to log outgoing HTTP requests to the LLM endpoint,
    // including request body to verify Cyrillic text is being sent correctly.
    internal sealed class LoggingHttpHandler : DelegatingHandler
    {
        public LoggingHttpHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"[LLM HTTP] {request.Method} {request.RequestUri}");

                if (request.Content != null)
                {
                    var contentType = request.Content.Headers.ContentType?.ToString() ?? "(none)";
                    string body = await request.Content.ReadAsStringAsync(cancellationToken);
                    Console.WriteLine($"[LLM HTTP] Content-Type: {contentType}");
                    Console.WriteLine($"[LLM HTTP] Body (first 800 chars): {Truncate(body, 800)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LLM HTTP] Logging error: {ex.Message}");
            }

            var response = await base.SendAsync(request, cancellationToken);

            try
            {
                Console.WriteLine($"[LLM HTTP] Response: {(int)response.StatusCode} {response.ReasonPhrase}");
            }
            catch { /* ignore */ }

            return response;
        }

        private static string Truncate(string text, int maxLen)
            => text == null ? string.Empty : (text.Length > maxLen ? text.Substring(0, maxLen) + "..." : text);
    }
}