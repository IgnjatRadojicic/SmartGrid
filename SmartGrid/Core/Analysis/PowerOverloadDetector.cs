using System;
using System.Collections.Generic;
using Contracts.DTOs;
using Core.Events;

namespace Core.Analysis
{
    // Implementacija iz specifikacije:
    // P(t) = V(t) * I(t)
    // Ako je P(t) > P_max_threshold, podici dogadjaj
    public class PowerOverloadDetector
    {
        private readonly GridEventManager _eventManager;
        public double PowerMaxThreshold { get; set; }

        // Dataset power range: 1.9 - 5.3 kW
        // V * I daje vece vrednosti (200-260V * 7-22A = 1500-5700W)
        // Prag za preopterecenje: npr 4000W
        public PowerOverloadDetector(GridEventManager eventManager, double threshold = 4000.0)
        {
            _eventManager = eventManager;
            PowerMaxThreshold = threshold;
        }

        // Prolazi kroz sve readings i proverava P(t) = V(t) * I(t)
        public List<AnomalyResultDto> Detect(List<GridReadingDto> readings)
        {
            var anomalies = new List<AnomalyResultDto>();

            if (readings == null || readings.Count == 0)
                return anomalies;

            foreach (var reading in readings)
            {
                double calculatedPower = reading.Voltage * reading.Current;

                if (calculatedPower > PowerMaxThreshold)
                {
                    string severity;
                    if (calculatedPower > PowerMaxThreshold * 1.3)
                        severity = "Critical";
                    else if (calculatedPower > PowerMaxThreshold * 1.15)
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
                        Threshold = PowerMaxThreshold,
                        AffectedNode = "Node1",
                        Description = string.Format("P(t) = V({0:F1}) * I({1:F1}) = {2:F1}W (prag: {3}W)",
                            reading.Voltage, reading.Current, calculatedPower, PowerMaxThreshold)
                    };

                    anomalies.Add(anomaly);

                    _eventManager.RaiseAnomalyDetected(
                        new AnomalyEventArgs(anomaly, reading));
                }
            }

            return anomalies;
        }

        // Provera jednog readinga - za real-time
        public AnomalyResultDto CheckSingle(GridReadingDto reading, string nodeId = "Node1")
        {
            double calculatedPower = reading.Voltage * reading.Current;

            if (calculatedPower <= PowerMaxThreshold)
                return null;

            string severity;
            if (calculatedPower > PowerMaxThreshold * 1.3)
                severity = "Critical";
            else if (calculatedPower > PowerMaxThreshold * 1.15)
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
                Threshold = PowerMaxThreshold,
                AffectedNode = nodeId,
                Description = string.Format("{0}: P = {1:F1}W preopterecenje", nodeId, calculatedPower)
            };

            _eventManager.RaiseAnomalyDetected(new AnomalyEventArgs(anomaly, reading));
            return anomaly;
        }
    }
}