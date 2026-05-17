using System.Collections.Generic;
using Contracts.DTOs;
using Contracts.Interfaces;
using Core.Analysis;
using Core.ExternalApi;
using Data.Repository;

namespace Service
{
    public class WeatherService : IWeatherService
    {
        private static OpenWeatherClient _weatherClient;
        private static PowerOverloadDetector _powerDetector;
        private static GridDataRepository _repo;

        public static void Initialize(
            OpenWeatherClient weatherClient,
            PowerOverloadDetector powerDetector,
            GridDataRepository repo)
        {
            _weatherClient = weatherClient;
            _powerDetector = powerDetector;
            _repo = repo;
        }

        public WeatherDataDto GetCurrentWeather(string city)
        {
            return _weatherClient.GetCurrentWeather(city);
        }

        public List<WeatherDataDto> GetWeatherForAllNodes()
        {
            var results = new List<WeatherDataDto>();
            foreach (var kvp in GridDataRepository.Nodes)
            {
                results.Add(_weatherClient.GetCurrentWeather(kvp.Value.City));
            }
            return results;
        }

        public WeatherCorrelationDto GetWeatherCorrelation()
        {
            var weatherData = GetWeatherForAllNodes();
            var readings = _repo.GetAllReadings();
            return _powerDetector.GenerateWeatherCorrelation(readings, weatherData);
        }
    }
}