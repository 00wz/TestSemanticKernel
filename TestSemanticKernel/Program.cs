using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using dotenv.net;
using System.IO;
using System.Text;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace TestSemanticKernel
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Ensure Windows console uses UTF-8 code page for proper input/output of Cyrillic
            ConsoleEncoding.UseUtf8();
            
            // Explicitly load .env from build (output/bin) directory
            var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { envPath }, overwriteExistingVars: true));

            // Initialize Semantic Kernel
            var kernel = KernelService.CreateKernel();

            // Import OpenAPI plugin limited to one operation
            await ImportVmtpOpenApiAsync(kernel);

            // Register tools (WeatherTool, VmtpTool)
            kernel.Plugins.AddFromType<WeatherTool>();
            kernel.Plugins.AddFromType<VmtpTool>();
            
            // Diagnostic: print all registered functions
            Console.WriteLine("Registered functions:");
            foreach (var function in kernel.Plugins.GetFunctionsMetadata())
            {
                Console.WriteLine($"- [{function.PluginName}] {function.Name}");
            }

            // Start chat session
            var chat = new ChatSession(kernel);
            await chat.RunAsync();
        }

        private static async Task ImportVmtpOpenApiAsync(Kernel kernel)
        {
            try
            {
                var specPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VMTPSpec.json");
                if (!File.Exists(specPath))
                {
                    Console.WriteLine($"OpenAPI spec not found: {specPath}");
                    return;
                }

                var baseUrl = Environment.GetEnvironmentVariable("VMTP_API_BASE_URL") ?? "http://10.61.16.12";
                var token = Environment.GetEnvironmentVariable("VMTP_API_TOKEN");

                var exec = new OpenApiFunctionExecutionParameters
                {
                    ServerUrlOverride = new Uri(baseUrl),
                    EnableDynamicPayload = true,
                    EnablePayloadNamespacing = true,
                    AuthCallback = (request, ct) =>
                    {
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        }
                        return Task.CompletedTask;
                    }
                };

                // Сформируем минимальную OpenAPI-спецификацию с ОДНИМ методом: GET /api/VMTPBaseObjectValue/GetByFilter
                // Чтобы не зависеть от внутренних API парсера, отфильтруем JSON сами и импортируем через поток.
                var originalJson = await File.ReadAllTextAsync(specPath);
                var root = JsonNode.Parse(originalJson) as JsonObject;
                if (root is null)
                {
                    Console.WriteLine("Failed to parse OpenAPI JSON.");
                }
                else
                {
                    var paths = root["paths"] as JsonObject;
                    if (paths is not null)
                    {
                        var key = "/api/VMTPBaseObjectValue/GetByFilter";
                        var filteredPaths = new JsonObject();

                        if (paths.TryGetPropertyValue(key, out var node) && node is JsonObject pathObj && pathObj.TryGetPropertyValue("get", out var getNode) && getNode is not null)
                        {
                            // Сохраняем только GET для нужного пути
                            var onlyGet = new JsonObject
                            {
                                ["get"] = getNode.DeepClone()
                            };
                            filteredPaths[key] = onlyGet;

                            // Заменяем раздел paths на отфильтрованный
                            root["paths"] = filteredPaths;

                            // Санитизируем имена схем в components/schemas и обновим все $ref
                            SanitizeOpenApiComponentsAndRefs(root);

                            // Сериализуем в память
                            using var ms = new MemoryStream();
                            await using (var writer = new StreamWriter(ms, new UTF8Encoding(false), 1024, leaveOpen: true))
                            {
                                await writer.WriteAsync(root.ToJsonString());
                            }
                            ms.Position = 0;

#pragma warning disable SKEXP0040
                            await kernel.ImportPluginFromOpenApiAsync(
                                pluginName: "vmtp",
                                stream: ms,
                                executionParameters: exec
                            );
#pragma warning restore SKEXP0040

                            Console.WriteLine("Imported OpenAPI plugin 'vmtp' with single operation: GET /api/VMTPBaseObjectValue/GetByFilter");
                            return;
                        }
                    }
                }

                // Fallback: import full spec if the operation wasn't found
#pragma warning disable SKEXP0040
                // Попытка с полной спеки: предварительно санитизируем компоненты, чтобы избежать ошибок парсера
                var originalJson2 = await File.ReadAllTextAsync(specPath);
                var root2 = JsonNode.Parse(originalJson2) as JsonObject;
                if (root2 is not null)
                {
                    SanitizeOpenApiComponentsAndRefs(root2);
                    using var ms2 = new MemoryStream();
                    await using (var writer2 = new StreamWriter(ms2, new UTF8Encoding(false), 1024, leaveOpen: true))
                    {
                        await writer2.WriteAsync(root2.ToJsonString());
                    }
                    ms2.Position = 0;

                    await kernel.ImportPluginFromOpenApiAsync(pluginName: "vmtp", stream: ms2, executionParameters: exec);
                    Console.WriteLine("Imported OpenAPI plugin 'vmtp' from sanitized full spec (fallback).");
                }
                else
                {
                    Console.WriteLine("Failed to parse OpenAPI JSON for fallback import.");
                }
#pragma warning restore SKEXP0040
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to import OpenAPI plugin: {ex.Message}");
            }
        }

        // Санитизация: переименовываем ключи components.schemas к допустимому виду и обновляем все $ref
        private static void SanitizeOpenApiComponentsAndRefs(JsonObject root)
        {
            if (root is null) return;

            var components = root["components"] as JsonObject;
            var schemas = components?["schemas"] as JsonObject;
            if (schemas is null) return;

            // Собираем пары (староеИмя -> значение)
            var originals = new List<(string key, JsonNode? value)>();
            foreach (var kv in schemas)
            {
                originals.Add((kv.Key, kv.Value));
            }

            // Правило санитизации: оставить только [a-zA-Z0-9.\-_] и ограничить длину
            string Sanitize(string name)
            {
                var cleaned = Regex.Replace(name, "[^a-zA-Z0-9\\.\\-_]", "");
                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    cleaned = "Schema";
                }
                if (cleaned.Length > 128) cleaned = cleaned.Substring(0, 128);
                return cleaned;
            }

            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var used = new HashSet<string>(StringComparer.Ordinal);

            foreach (var (oldKey, _) in originals)
            {
                var candidate = Sanitize(oldKey);
                // Гарантируем уникальность
                if (!used.Add(candidate))
                {
                    var i = 2;
                    var next = candidate + "_" + i;
                    while (!used.Add(next)) { i++; next = candidate + "_" + i; }
                    candidate = next;
                }
                map[oldKey] = candidate;
            }

            // Если нет изменений — ничего не делаем
            var anyChanges = map.Any(kv => kv.Key != kv.Value);
            if (anyChanges)
            {
                // Пересобираем объект schemas с новыми ключами
                var newSchemas = new JsonObject();
                foreach (var (oldKey, value) in originals)
                {
                    var newKey = map[oldKey];
                    newSchemas[newKey] = value?.DeepClone();
                }
                components!["schemas"] = newSchemas;

                // Обновляем все $ref во всём документе
                void RewriteRefs(JsonNode? node)
                {
                    if (node is JsonObject obj)
                    {
                        foreach (var prop in obj.ToList())
                        {
                            if (prop.Key == "$ref" && prop.Value is JsonValue jv && jv.TryGetValue<string>(out var refVal) && !string.IsNullOrEmpty(refVal))
                            {
                                // Ищем #/components/schemas/{oldKey}
                                foreach (var kv in map)
                                {
                                    var oldRef = "#/components/schemas/" + kv.Key;
                                    if (refVal == oldRef)
                                    {
                                        obj["$ref"] = "#/components/schemas/" + kv.Value;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                RewriteRefs(prop.Value);
                            }
                        }
                    }
                    else if (node is JsonArray arr)
                    {
                        foreach (var item in arr)
                        {
                            RewriteRefs(item);
                        }
                    }
                }

                RewriteRefs(root);
            }
        }
    }
}
