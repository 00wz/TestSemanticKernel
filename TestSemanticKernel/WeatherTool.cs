using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace TestSemanticKernel
{
    public class WeatherTool
    {
        [KernelFunction]
        [Description("Get the current weather in the specified city")]
        public Task<string> GetWeatherAsync(
            [Description("The name of the city to get the weather for")] string city)
        {
            // Test implementation (stub)
            var weather = city.ToLower() switch
            {
                "москва" or "moscow" => "Moscow: +5°C, cloudy",
                "санкт-петербург" or "saint petersburg" or "st. petersburg" => "Saint Petersburg: +3°C, rainy",
                "новосибирск" or "novosibirsk" => "Novosibirsk: -2°C, snow",
                _ => $"{city}: +10°C, clear"
            };
            return Task.FromResult(weather);
        }
    }
}
