using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Threading.Tasks;

namespace TestSemanticKernel
{
    public class ChatSession
    {
        private readonly Kernel _kernel;
        private readonly ChatHistory _history = new();

        public ChatSession(Kernel kernel)
        {
            _kernel = kernel;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("AI chat. Type 'exit' to quit.");
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            // Enable function calling for OpenAI-compatible models
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            while (true)
            {
                Console.Write("You: ");
                var userInput = Console.ReadLine();
                if (userInput == null || userInput.Trim().ToLower() == "exit")
                    break;

                // Add user message to history
                _history.AddUserMessage(userInput);

                // Get AI response with function calling enabled
                var aiMessage = (await chatService.GetChatMessageContentAsync(
                    _history,
                    executionSettings: executionSettings,
                    kernel: _kernel
                )).Content?.Trim();

                if (!string.IsNullOrEmpty(aiMessage))
                {
                    Console.WriteLine($"AI: {aiMessage}");
                    _history.AddAssistantMessage(aiMessage);
                }
                else
                {
                    Console.WriteLine("AI did not return a response.");
                }
            }
        }
    }
}
