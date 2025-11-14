using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestSemanticKernel
{
    public class VmtpTool
    {
        private static readonly HttpClient _http = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        })
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private readonly string _baseUrl;
        private readonly string? _bearer;

        public VmtpTool()
        {
            _baseUrl = Environment.GetEnvironmentVariable("VMTP_API_BASE_URL")?.TrimEnd('/') ?? "http://10.61.16.12:3083";
            _bearer = Environment.GetEnvironmentVariable("VMTP_API_TOKEN");
        }

        [KernelFunction("getBaseObjectValuesByFilter")]
        [Description("Выполняет GET /api/VMTPBaseObjectValue/GetByFilter. Параметры фильтра — JSON-строка (ключи как в swagger). Возвращает JSON-строку: при успехе {ok:true,status,total?,data|dataRaw}, при ошибке {ok:false,...}.")]
        public async Task<string> GetBaseObjectValuesByFilterAsync(
            [Description("JSON-объект с параметрами фильтрации, например: {\"pageNumber\":1,\"pageSize\":50,\"withValues\":true}")]
            string queryJson)
        {
            try
            {
                var query = ParseJsonToFlatDictionary(queryJson);

                var urlBuilder = new StringBuilder();
                urlBuilder.Append(_baseUrl);
                urlBuilder.Append("/api/VMTPBaseObjectValue/GetByFilter");

                var first = true;
                foreach (var (key, values) in query)
                {
                    foreach (var v in values)
                    {
                        urlBuilder.Append(first ? '?' : '&');
                        first = false;
                        urlBuilder.Append(Uri.EscapeDataString(key));
                        urlBuilder.Append('=');
                        urlBuilder.Append(Uri.EscapeDataString(v));
                    }
                }

                var url = urlBuilder.ToString();

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrWhiteSpace(_bearer))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearer);
                }
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                var content = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                string? totalCount = null;
                if (resp.Headers.TryGetValues("X-Total-Count", out var totalValues))
                {
                    using var e = totalValues.GetEnumerator();
                    if (e.MoveNext()) totalCount = e.Current;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    return BuildErrorJson(
                        url: url,
                        status: (int)resp.StatusCode,
                        reason: resp.ReasonPhrase ?? string.Empty,
                        body: Truncate(content, 2000));
                }

                return BuildSuccessJson(
                    url: url,
                    status: (int)resp.StatusCode,
                    total: totalCount,
                    body: content);
            }
            catch (TaskCanceledException ex)
            {
                var isTimeout = !ex.CancellationToken.IsCancellationRequested;
                return BuildExceptionJson("TaskCanceledException", isTimeout ? "Request timeout" : "Request canceled", ex.Message);
            }
            catch (HttpRequestException ex)
            {
                return BuildExceptionJson("HttpRequestException", "HTTP request failed", ex.Message);
            }
            catch (JsonException ex)
            {
                return BuildExceptionJson("JsonException", "Invalid queryJson", ex.Message);
            }
            catch (Exception ex)
            {
                return BuildExceptionJson(ex.GetType().Name, "Unhandled error", ex.Message);
            }
        }

        private static string Truncate(string s, int max)
            => s.Length <= max ? s : s.Substring(0, max);

        private static Dictionary<string, List<string>> ParseJsonToFlatDictionary(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new();

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("queryJson должен быть JSON-объектом");

            var dict = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var key = prop.Name; // поддерживаем ключи с точками, как 'propertyQuery.propertyId'
                var el = prop.Value;

                if (el.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in el.EnumerateArray())
                    {
                        Add(dict, key, JsonElementToString(item));
                    }
                }
                else if (el.ValueKind == JsonValueKind.Null || el.ValueKind == JsonValueKind.Undefined)
                {
                    // пропускаем
                }
                else
                {
                    Add(dict, key, JsonElementToString(el));
                }
            }

            return dict;

            static void Add(Dictionary<string, List<string>> d, string k, string v)
            {
                if (!d.TryGetValue(k, out var list))
                {
                    list = new List<string>();
                    d[k] = list;
                }
                list.Add(v);
            }
        }

        private static string JsonElementToString(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.String:
                    return el.GetString() ?? "";
                case JsonValueKind.Number:
                    return el.GetRawText();
                case JsonValueKind.True:
                    return "true";
                case JsonValueKind.False:
                    return "false";
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return "";
                default:
                    // вложенные объекты/массивы сериализуем как компактную JSON-строку
                    return el.GetRawText();
            }
        }

        private static string BuildSuccessJson(string url, int status, string? total, string body)
        {
            // Пытаемся встроить ответ как JSON в поле "data". Если не JSON — вернем "dataRaw".
            try
            {
                using var doc = JsonDocument.Parse(body);
                using var ms = new MemoryStream();
                using (var writer = new Utf8JsonWriter(ms))
                {
                    writer.WriteStartObject();
                    writer.WriteBoolean("ok", true);
                    writer.WriteNumber("status", status);
                    writer.WriteString("url", url);
                    if (!string.IsNullOrEmpty(total))
                        writer.WriteString("total", total);
                    writer.WritePropertyName("data");
                    doc.RootElement.WriteTo(writer);
                    writer.WriteEndObject();
                }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
            catch
            {
                var opts = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
                var envelope = new
                {
                    ok = true,
                    status,
                    url,
                    total = string.IsNullOrEmpty(total) ? null : total,
                    dataRaw = body
                };
                return JsonSerializer.Serialize(envelope, opts);
            }
        }

        private static string BuildErrorJson(string url, int status, string reason, string body)
        {
            var opts = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            var envelope = new
            {
                ok = false,
                status,
                reason,
                url,
                body
            };
            return JsonSerializer.Serialize(envelope, opts);
        }

        private static string BuildExceptionJson(string error, string message, string detail)
        {
            var opts = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            var envelope = new
            {
                ok = false,
                error,
                message,
                detail
            };
            return JsonSerializer.Serialize(envelope, opts);
        }
    }
}