using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Threading.Tasks;
using System.Net;

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

            // Переключатель function calling через переменную окружения SK_ENABLE_FUNCTION_CALLING (false = выключить)
            var enableFunctions = !string.Equals(
                Environment.GetEnvironmentVariable("SK_ENABLE_FUNCTION_CALLING"),
                "false",
                StringComparison.OrdinalIgnoreCase);

            var executionSettings = new OpenAIPromptExecutionSettings();
            if (enableFunctions)
            {
                executionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();
            }

            while (true)
            {
                Console.Write("You: ");
                var userInput = Console.ReadLine();
                if (userInput == null || userInput.Trim().ToLower() == "exit")
                    break;

                // Add user message to history
                _history.AddUserMessage(userInput);

                // Get AI response with function calling enabled
                string? aiMessage = null;
                try
                {
                    var result = await chatService.GetChatMessageContentAsync(
                        _history,
                        executionSettings: executionSettings,
                        kernel: _kernel
                    );
                    aiMessage = result.Content?.Trim();
                }
                catch (Microsoft.SemanticKernel.HttpOperationException ex) when (enableFunctions && ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Некоторые OpenAI-совместимые эндпоинты не поддерживают tool/function calling → отключаем и повторяем
                    Console.WriteLine("AI error 400 Bad Request on function-calling. Disabling function-calling and retrying...");
                    enableFunctions = false;
                    executionSettings.FunctionChoiceBehavior = null;

                    try
                    {
                        var retry = await chatService.GetChatMessageContentAsync(
                            _history,
                            executionSettings: executionSettings,
                            kernel: _kernel
                        );
                        aiMessage = retry.Content?.Trim();
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"AI error after fallback: {ex2.Message}");
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AI error: {ex.Message}");
                    continue;
                }

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
