using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Net.Http;

namespace TestSemanticKernel
{
    public static class KernelService
    {
        public static Kernel CreateKernel()
        {
            // Get environment variables
            var baseUrl = Environment.GetEnvironmentVariable("LOCAL_LLM_BASE_URL");
            var apiKey = Environment.GetEnvironmentVariable("LOCAL_LLM_API_KEY");
            var model = Environment.GetEnvironmentVariable("LOCAL_LLM_MODEL");

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
                throw new InvalidOperationException("LLM environment variables are not set (LOCAL_LLM_BASE_URL, LOCAL_LLM_API_KEY, LOCAL_LLM_MODEL)");

            // Create HttpClient with logging handler to verify request bodies include Cyrillic text
            var inner = new HttpClientHandler();
            var httpClient = new HttpClient(new LoggingHttpHandler(inner))
            {
                BaseAddress = new Uri(baseUrl)
            };
            httpClient.DefaultRequestHeaders.AcceptCharset.Clear();
            httpClient.DefaultRequestHeaders.AcceptCharset.ParseAdd("utf-8");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            // SK usually sets Content-Type per request; keep a fallback header
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");

            // Create Semantic Kernel with OpenAI-compatible endpoint
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(
                model,
                apiKey,
                orgId: null,
                serviceId: null,
                httpClient: httpClient
            );
            return builder.Build();
        }
    }
}
