using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.DTOs;

namespace Core.Analysis
{
    // Simple Moving Average predikcija potrosnje
    // Koristi PowerUsage iz dataseta za forecast
    public class ConsumptionPredictor
    {
        private readonly int _windowSize;

        public ConsumptionPredictor(int windowSize = 50)
        {
            _windowSize = windowSize;
        }

        public ConsumptionForecastDto Predict(List<GridReadingDto> readings, string nodeId, int forecastPoints = 20)
        {
            if (readings == null || readings.Count < _windowSize)
                return new ConsumptionForecastDto
                {
                    NodeId = nodeId,
                    ForecastPoints = new List<ForecastPointDto>(),
                    GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
                };

            var powerValues = readings.Select(r => r.PowerUsage).ToList();

            // Poslednji SMA prozor
            var recentValues = powerValues.Skip(powerValues.Count - _windowSize).Take(_windowSize).ToList();
            double currentSma = recentValues.Average();

            // Trend: razlika izmedju poslednja dva prozora
            double trend = 0;
            if (powerValues.Count >= _windowSize * 2)
            {
                var previousWindow = powerValues
                    .Skip(powerValues.Count - _windowSize * 2)
                    .Take(_windowSize)
                    .Average();
                trend = (currentSma - previousWindow) / _windowSize;
            }

            var forecastList = new List<ForecastPointDto>();
            var lastTimestamp = readings.Last().Timestamp;

            for (int i = 1; i <= forecastPoints; i++)
            {
                double predicted = currentSma + (trend * i);
                double confidence = Math.Max(0.3, 1.0 - (i * 0.03));

                forecastList.Add(new ForecastPointDto
                {
                    Timestamp = lastTimestamp.AddMinutes(i).ToString("yyyy-MM-ddTHH:mm:ss"),
                    PredictedPower = Math.Round(predicted, 4),
                    Confidence = Math.Round(confidence, 2)
                });
            }

            return new ConsumptionForecastDto
            {
                NodeId = nodeId,
                ForecastPoints = forecastList,
                GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
            };
        }
    }
}