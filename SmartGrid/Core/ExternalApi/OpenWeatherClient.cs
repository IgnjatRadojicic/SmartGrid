using System;
using System.ComponentModel;
using System.Net;
using System.Text;
using Contracts.DTOs;

namespace Core.ExternalApi
{
    // OpenWeatherMap API klijent za .NET 4.7.2 (
    public class OpenWeatherClient
    {
        private readonly string _apiKey;
        private const string BaseUrl = "https://api.openweathermap.org/data/2.5/weather";

        public OpenWeatherClient(string apiKey)
        {
            _apiKey = apiKey;
        }

        public WeatherDataDto GetCurrentWeather(string city)
        {
            try
            {
                string url = string.Format("{0}?q={1},RS&appid={2}&units=metric", BaseUrl, city, _apiKey);

                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    string json = client.DownloadString(url);

                    return ParseWeatherJson(json, city);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[OpenWeather] Greska za " + city + ": " + ex.Message);
                return new WeatherDataDto
                {
                    City = city,
                    Description = "Nedostupno",
                    FetchedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
                };
            }
        }

        private WeatherDataDto ParseWeatherJson(string json, string city)
        {
            var dto = new WeatherDataDto
            {
                City = city,
                FetchedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
            };

            dto.Temperature = ExtractDouble(json, "\"temp\":");
            dto.Humidity = ExtractDouble(json, "\"humidity\":");
            dto.Pressure = ExtractDouble(json, "\"pressure\":");
            dto.WindSpeed = ExtractDouble(json, "\"speed\":");
            dto.Description = ExtractString(json, "\"description\":\"");

            return dto;
        }



        private double ExtractDouble(string json, string key)
        {
            int idx = json.IndexOf(key);
            if (idx < 0) return 0;
            idx += key.Length;
            int end = idx;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.' || json[end] == '-'))
                end++;
            double result;
            double.TryParse(json.Substring(idx, end - idx),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out result);
            return result;
        }

        private string ExtractString(string json, string key)
        {
            int idx = json.IndexOf(key);
            if (idx < 0) return "";
            idx += key.Length;
            int end = json.IndexOf("\"", idx);
            if (end < 0) return "";
            return json.Substring(idx, end - idx);
        }
    }
}