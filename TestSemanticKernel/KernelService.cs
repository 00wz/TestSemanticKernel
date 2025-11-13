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

            // Create plain HttpClient; let SK/OpenAI connector manage headers and encodings per request
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };

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
