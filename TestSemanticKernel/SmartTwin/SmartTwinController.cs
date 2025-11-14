using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DigitalAssistant.SmartTwinMcp.Controllers;

/// <summary>
/// Контроллер для взаимодействия со SmartTwin
/// </summary>
public class SmartTwinController
{
    /// <summary>
    /// Поиск объектов Цифрового Двойника
    /// </summary>
    /// <param name="name">Название объекта</param>
    /// <param name="layerId">Слой объекта</param>
    /// <returns>Коллекция свойств объекта</returns>
    [HttpGet]
    [KernelFunction]
    [Description("Поиск объектов по имени")]
    public async Task<IReadOnlyCollection<VMTPBaseObjectResponseItem>> SearchObject(
        [Description("Имя объекта")] string name, 
        [Description("Идентификатор слоя (если есть)")] Guid? layerId = null)
    {
        using var http = new HttpClient();

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("http://10.61.16.12:3081/api/oauth/token"),
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", "test_supervisor" },
                { "password", "123" },
                { "client_id", "9D0F5B08-072B-45CC-97A0-B0EBE465D327" },
                { "scope", "full" },
            }),
        };

        using (var response = await http.SendAsync(request))
        {
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

            http.DefaultRequestHeaders.Authorization = new("Bearer", tokenResponse!.AccessToken);
        }

        var layerIdQuery = string.Empty;

        if (layerId.HasValue)
        {
            layerIdQuery = $"LayerIdList={layerId.Value}";
        }

        var query = $"http://10.61.16.12:3083/api/VMTPBaseObjectValue/GetByFilter?Name={name}&{layerIdQuery}&WithValues=True&WithMetadata=True&WithGeoData=True&Deleted=False";

        var result = await http.GetFromJsonAsync<List<VMTPBaseObjectResponseItem>>(query);

        return result ?? new();
    }

    class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }
    }
}
