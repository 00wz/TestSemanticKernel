using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using dotenv.net;
using System.IO;
using System.Text;

namespace TestSemanticKernel
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Set console encoding to UTF-8 for correct Unicode (including Russian) support
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            // Explicitly load .env from build (output/bin) directory
            var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { envPath }, overwriteExistingVars: true));

            // Print environment variables for debugging
            Console.WriteLine("DEBUG: LOCAL_LLM_BASE_URL = " + Environment.GetEnvironmentVariable("LOCAL_LLM_BASE_URL"));
            Console.WriteLine("DEBUG: LOCAL_LLM_API_KEY = " + Environment.GetEnvironmentVariable("LOCAL_LLM_API_KEY"));
            Console.WriteLine("DEBUG: LOCAL_LLM_MODEL = " + Environment.GetEnvironmentVariable("LOCAL_LLM_MODEL"));

            // Initialize Semantic Kernel
            var kernel = KernelService.CreateKernel();

            // Register tools (WeatherTool)
            kernel.Plugins.AddFromType<WeatherTool>();

            // Diagnostic: print all registered functions
            Console.WriteLine("Registered functions:");
            foreach (var function in kernel.Plugins.GetFunctionsMetadata())
            {
                Console.WriteLine($"- [{function.PluginName}] {function.Name}");
            }

            // Start chat session
            var chat = new ChatSession(kernel);
            await chat.RunAsync();

            // IMPORTANT: Make sure your .env and all source files are saved in UTF-8 encoding!
        }
    }
}
