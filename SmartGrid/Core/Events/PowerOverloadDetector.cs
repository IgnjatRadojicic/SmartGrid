using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.DTOs;
using Core.Events;

namespace Core.Analysis
{
    // Implementacija iz specifikacije:
    // P(t) = V(t) * I(t)
    // Ako je P(t) > P_max_threshold, podici dogadjaj
    //
    // Dodatno: weather korelacija koriguje prag
    // i Z-score detektuje outliere u power vrednostima
    public class PowerOverloadDetector
    {
        private readonly GridEventManager _eventManager;
        public double PowerMaxThreshold { get; set; }

        // Weather korelacija konstante
        private const double ComfortTempLow = 15.0;
        private const double ComfortTempHigh = 25.0;
        private const double TempImpactFactor = 0.03;

        public PowerOverloadDetector(GridEventManager eventManager, double threshold = 4000.0)
        {
            _eventManager = eventManager;
            PowerMaxThreshold = threshold;
        }

        // ==================== P(t) = V(t) * I(t) DETEKCIJA ====================

        // Prolazi kroz sve readings i proverava P(t) = V(t) * I(t)
        public List<AnomalyResultDto> Detect(List<GridReadingDto> readings)
        {
            return Detect(readings, null);
        }

        // Overload sa weather podacima - koriguje prag
        public List<AnomalyResultDto> Detect(List<GridReadingDto> readings, WeatherDataDto weather)
        {
            var anomalies = new List<AnomalyResultDto>();

            if (readings == null || readings.Count == 0)
                return anomalies;

            // Koriguj prag na osnovu vremena
            double activeThreshold = PowerMaxThreshold;
            if (weather != null)
            {
                double riskFactor = CalculateWeatherRiskFactor(weather);
                activeThreshold = AdjustThreshold(PowerMaxThreshold, riskFactor);
            }

            foreach (var reading in readings)
            {
                double calculatedPower = reading.Voltage * reading.Current;

                if (calculatedPower > activeThreshold)
                {
                    string severity;
                    if (calculatedPower > activeThreshold * 1.3)
                        severity = "Critical";
                    else if (calculatedPower > activeThreshold * 1.15)
                        severity = "High";
                    else
                        severity = "Medium";

                    var anomaly = new AnomalyResultDto
                    {
                        ReadingId = reading.Id,
                        DetectedAt = reading.TimestampFormatted,
                        AnomalyType = "PowerOverload",
                        Severity = severity,
                        Value = Math.Round(calculatedPower, 2),
                        Threshold = activeThreshold,
                        AffectedNode = "Node1",
                        Description = string.Format("P(t) = V({0:F1}) * I({1:F1}) = {2:F1}W (prag: {3:F0}W)",
                            reading.Voltage, reading.Current, calculatedPower, activeThreshold)
                    };

                    anomalies.Add(anomaly);

                    _eventManager.RaiseAnomalyDetected(
                        new AnomalyEventArgs(anomaly, reading));
                }
            }

            return anomalies;
        }

        // Provera jednog readinga - za real-time polling
        public AnomalyResultDto CheckSingle(GridReadingDto reading, string nodeId, WeatherDataDto weather = null)
        {
            double activeThreshold = PowerMaxThreshold;
            if (weather != null)
            {
                double riskFactor = CalculateWeatherRiskFactor(weather);
                activeThreshold = AdjustThreshold(PowerMaxThreshold, riskFactor);
            }

            double calculatedPower = reading.Voltage * reading.Current;

            if (calculatedPower <= activeThreshold)
                return null;

            string severity;
            if (calculatedPower > activeThreshold * 1.3)
                severity = "Critical";
            else if (calculatedPower > activeThreshold * 1.15)
                severity = "High";
            else
                severity = "Medium";

            var anomaly = new AnomalyResultDto
            {
                ReadingId = reading.Id,
                DetectedAt = reading.TimestampFormatted,
                AnomalyType = "PowerOverload",
                Severity = severity,
                Value = Math.Round(calculatedPower, 2),
                Threshold = activeThreshold,
                AffectedNode = nodeId,
                Description = string.Format("{0}: P = {1:F1}W (prag: {2:F0}W)", nodeId, calculatedPower, activeThreshold)
            };

            _eventManager.RaiseAnomalyDetected(new AnomalyEventArgs(anomaly, reading));
            return anomaly;
        }

        // Z-score anomaly detection na P(t) = V*I vrednostima
        public List<AnomalyResultDto> DetectZScoreAnomalies(
            List<GridReadingDto> readings, double zScoreThreshold = 2.0)
        {
            var anomalies = new List<AnomalyResultDto>();

            if (readings == null || readings.Count < 2)
                return anomalies;

            var powerValues = readings.Select(r => r.Voltage * r.Current).ToList();
            double mean = powerValues.Average();
            double stdDev = CalculateStdDev(powerValues, mean);

            if (stdDev == 0) return anomalies;

            for (int i = 0; i < readings.Count; i++)
            {
                double zScore = (powerValues[i] - mean) / stdDev;

                if (Math.Abs(zScore) > zScoreThreshold)
                {
                    string severity;
                    if (Math.Abs(zScore) > zScoreThreshold * 1.75)
                        severity = "Critical";
                    else if (Math.Abs(zScore) > zScoreThreshold * 1.5)
                        severity = "High";
                    else if (Math.Abs(zScore) > zScoreThreshold * 1.25)
                        severity = "Medium";
                    else
                        severity = "Low";

                    var anomaly = new AnomalyResultDto
                    {
                        ReadingId = readings[i].Id,
                        DetectedAt = readings[i].TimestampFormatted,
                        AnomalyType = "PowerZScore",
                        Severity = severity,
                        Value = Math.Round(powerValues[i], 2),
                        Threshold = Math.Round(mean + (zScoreThreshold * stdDev), 2),
                        AffectedNode = "Node1",
                        Description = string.Format("P = {0:F1}W, Z-score = {1:F2} (prag: {2})",
                            powerValues[i], zScore, zScoreThreshold)
                    };

                    anomalies.Add(anomaly);

                    _eventManager.RaiseAnomalyDetected(
                        new AnomalyEventArgs(anomaly, readings[i]));
                }
            }

            return anomalies;
        }

        // Temperatura van komforne zone povecava potrosnju
        // Vraca multiplier: 1.0 = normalan, >1.0 = povecan rizik
        public double CalculateWeatherRiskFactor(WeatherDataDto weather)
        {
            if (weather == null || weather.Description == "Nedostupno")
                return 1.0;

            double risk = 1.0;

            if (weather.Temperature < ComfortTempLow)
                risk += (ComfortTempLow - weather.Temperature) * TempImpactFactor;
            else if (weather.Temperature > ComfortTempHigh)
                risk += (weather.Temperature - ComfortTempHigh) * TempImpactFactor;

            if (weather.WindSpeed > 15.0)
                risk += 0.1;

            return Math.Round(risk, 3);
        }

        // Veci rizik = nizi prag = osetljivija detekcija
        public double AdjustThreshold(double baseThreshold, double riskFactor)
        {
            double adjustment = 1.0 - ((riskFactor - 1.0) * 0.3);
            adjustment = Math.Max(0.7, Math.Min(1.0, adjustment));
            return Math.Round(baseThreshold * adjustment, 2);
        }

        public WeatherCorrelationDto GenerateWeatherCorrelation(
            List<GridReadingDto> readings,
            List<WeatherDataDto> weatherData)
        {
            var result = new WeatherCorrelationDto
            {
                GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                NodeCorrelations = new List<NodeWeatherCorrelationDto>()
            };

            if (readings == null || weatherData == null)
                return result;

            double avgPower = readings.Average(r => r.Voltage * r.Current);

            foreach (var weather in weatherData)
            {
                double riskFactor = CalculateWeatherRiskFactor(weather);
                double estimatedPower = avgPower * riskFactor;
                double adjustedThreshold = AdjustThreshold(PowerMaxThreshold, riskFactor);

                result.NodeCorrelations.Add(new NodeWeatherCorrelationDto
                {
                    City = weather.City,
                    Temperature = weather.Temperature,
                    Humidity = weather.Humidity,
                    WindSpeed = weather.WindSpeed,
                    WeatherDescription = weather.Description,
                    RiskFactor = riskFactor,
                    AvgPowerUsage = Math.Round(avgPower, 2),
                    EstimatedPowerUsage = Math.Round(estimatedPower, 2),
                    RiskLevel = riskFactor > 1.3 ? "High" : riskFactor > 1.15 ? "Medium" : "Low"
                });

                if (riskFactor > 1.3)
                {
                    _eventManager.RaiseThresholdExceeded(new ThresholdEventArgs(
                        weather.City, "WeatherRisk", riskFactor, 1.3));
                }
            }

            return result;
        }

        private double CalculateStdDev(List<double> values, double mean)
        {
            double sumSquares = values.Sum(v => Math.Pow(v - mean, 2));
            return Math.Sqrt(sumSquares / values.Count);
        }
    }
}