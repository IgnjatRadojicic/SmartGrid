using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.DTOs;
using Core.Events;

namespace Core.Analysis
{
    // delta_f = f(t) - f(t - delta_t)
    // Ako je |delta_f| > F_threshold, podici dogadjaj
    //
    // Dodatno: Z-score na frequency vrednosti
    public class FrequencyChangeDetector
    {
        private readonly GridEventManager _eventManager;
        public double FrequencyThreshold { get; set; }

        public FrequencyChangeDetector(GridEventManager eventManager, double threshold = 1.0)
        {
            _eventManager = eventManager;
            FrequencyThreshold = threshold;
        }

        // Prolazi kroz listu i poredi svaki red sa prethodnim
        public List<AnomalyResultDto> Detect(List<GridReadingDto> readings)
        {
            var anomalies = new List<AnomalyResultDto>();

            if (readings == null || readings.Count < 2)
                return anomalies;

            for (int i = 1; i < readings.Count; i++)
            {
                double deltaF = readings[i].Frequency - readings[i - 1].Frequency;

                if (Math.Abs(deltaF) > FrequencyThreshold)
                {
                    string severity;
                    if (Math.Abs(deltaF) > FrequencyThreshold * 3)
                        severity = "Critical";
                    else if (Math.Abs(deltaF) > FrequencyThreshold * 2)
                        severity = "High";
                    else if (Math.Abs(deltaF) > FrequencyThreshold * 1.5)
                        severity = "Medium";
                    else
                        severity = "Low";

                    var anomaly = new AnomalyResultDto
                    {
                        ReadingId = readings[i].Id,
                        DetectedAt = readings[i].TimestampFormatted,
                        AnomalyType = "FrequencySpike",
                        Severity = severity,
                        Value = Math.Round(deltaF, 4),
                        Threshold = FrequencyThreshold,
                        AffectedNode = "Node1",
                        Description = string.Format("delta_f = {0:F4} Hz (prag: {1} Hz) u {2}",
                            deltaF, FrequencyThreshold, readings[i].TimestampFormatted)
                    };

                    anomalies.Add(anomaly);

                    _eventManager.RaiseAnomalyDetected(
                        new AnomalyEventArgs(anomaly, readings[i]));
                }
            }

            return anomalies;
        }

        // Provera jednog para - za real-time polling
        public AnomalyResultDto CheckSingle(GridReadingDto current, GridReadingDto previous, string nodeId = "Node1")
        {
            double deltaF = current.Frequency - previous.Frequency;

            if (Math.Abs(deltaF) <= FrequencyThreshold)
                return null;

            string severity;
            if (Math.Abs(deltaF) > FrequencyThreshold * 3)
                severity = "Critical";
            else if (Math.Abs(deltaF) > FrequencyThreshold * 2)
                severity = "High";
            else
                severity = "Medium";

            var anomaly = new AnomalyResultDto
            {
                ReadingId = current.Id,
                DetectedAt = current.TimestampFormatted,
                AnomalyType = "FrequencySpike",
                Severity = severity,
                Value = Math.Round(deltaF, 4),
                Threshold = FrequencyThreshold,
                AffectedNode = nodeId,
                Description = string.Format("{0}: delta_f = {1:F4} Hz", nodeId, deltaF)
            };

            _eventManager.RaiseAnomalyDetected(new AnomalyEventArgs(anomaly, current));
            return anomaly;
        }
        // Z-score anomaly detection na frequency vrednostima
        public List<AnomalyResultDto> DetectZScoreAnomalies(
            List<GridReadingDto> readings, double zScoreThreshold = 2.0)
        {
            var anomalies = new List<AnomalyResultDto>();

            if (readings == null || readings.Count < 2)
                return anomalies;

            var freqValues = readings.Select(r => r.Frequency).ToList();
            double mean = freqValues.Average();
            double stdDev = CalculateStdDev(freqValues, mean);

            if (stdDev == 0) return anomalies;

            for (int i = 0; i < readings.Count; i++)
            {
                double zScore = (freqValues[i] - mean) / stdDev;

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
                        AnomalyType = "FrequencyZScore",
                        Severity = severity,
                        Value = Math.Round(freqValues[i], 4),
                        Threshold = Math.Round(mean + (zScoreThreshold * stdDev), 4),
                        AffectedNode = "Node1",
                        Description = string.Format("f = {0:F2} Hz, Z-score = {1:F2} (prag: {2})",
                            freqValues[i], zScore, zScoreThreshold)
                    };

                    anomalies.Add(anomaly);

                    _eventManager.RaiseAnomalyDetected(
                        new AnomalyEventArgs(anomaly, readings[i]));
                }
            }

            return anomalies;
        }

        private double CalculateStdDev(List<double> values, double mean)
        {
            double sumSquares = values.Sum(v => Math.Pow(v - mean, 2));
            return Math.Sqrt(sumSquares / values.Count);
        }
    }
}