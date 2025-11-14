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

            // Включаем авто-вызов функций для OpenAI-совместимых моделей
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

                // Добавляем сообщение пользователя в историю
                _history.AddUserMessage(userInput);

                // Потоковая генерация ответа с немедленным выводом токенов
                Console.Write("AI: ");
                var sb = new System.Text.StringBuilder();

                try
                {
                    await foreach (var update in chatService.GetStreamingChatMessageContentsAsync(
                        _history,
                        executionSettings: executionSettings,
                        kernel: _kernel))
                    {
                        var token = update.Content;
                        if (!string.IsNullOrEmpty(token))
                        {
                            sb.Append(token);
                            Console.Write(token);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine($"[streaming error] {ex.Message}");
                }

                Console.WriteLine();

                var finalMessage = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(finalMessage))
                {
                    _history.AddAssistantMessage(finalMessage);
                }
                else
                {
                    Console.WriteLine("AI did not return a response.");
                }
            }
        }
    }
}
