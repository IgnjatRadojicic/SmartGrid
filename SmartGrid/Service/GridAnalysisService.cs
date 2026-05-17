using System;
using System.Collections.Generic;
using System.Globalization;
using Contracts.DTOs;
using Contracts.Interfaces;
using Core.Analysis;
using Core.Events;
using Data.Repository;
using Data.Streams;

namespace Service
{
    public class GridAnalysisService : IGridAnalysisService
    {
        private static GridDataRepository _repo;
        private static GridEventManager _eventManager;
        private static FrequencyChangeDetector _freqDetector;
        private static PowerOverloadDetector _powerDetector;
        private static ConsumptionPredictor _predictor;
        private static DataCompressor _compressor;

        // Inicijalizacija poziva se iz Program.cs jednom
        public static void Initialize(
            GridDataRepository repo,
            GridEventManager eventManager,
            FrequencyChangeDetector freqDetector,
            PowerOverloadDetector powerDetector,
            ConsumptionPredictor predictor,
            DataCompressor compressor)
        {
            _repo = repo;
            _eventManager = eventManager;
            _freqDetector = freqDetector;
            _powerDetector = powerDetector;
            _predictor = predictor;
            _compressor = compressor;
        }

        public List<GridReadingDto> GetReadings(string page, string pageSize)
        {
            int p = ParseInt(page, 1);
            int ps = ParseInt(pageSize, 50);
            return _repo.GetReadings(p, ps);
        }

        public GridReadingDto GetReadingById(string id)
        {
            int readingId = ParseInt(id, 0);
            return _repo.GetReadingById(readingId);
        }

        public StabilityReportDto GetStabilityReport()
        {
            var readings = _repo.GetAllReadings();
            var freqAnomalies = _freqDetector.Detect(readings);
            var powerAnomalies = _powerDetector.Detect(readings);

            int faultCount = 0, normalCount = 0, warningCount = 0;
            double sumV = 0, sumA = 0, sumP = 0, sumF = 0;
            double maxP = double.MinValue, minF = double.MaxValue, maxF = double.MinValue;

            foreach (var r in readings)
            {
                if (r.FaultIndicator == 1) faultCount++;
                else if (r.FaultIndicator == 2) warningCount++;
                else normalCount++;

                sumV += r.Voltage;
                sumA += r.Current;
                sumP += r.PowerUsage;
                sumF += r.Frequency;

                if (r.PowerUsage > maxP) maxP = r.PowerUsage;
                if (r.Frequency < minF) minF = r.Frequency;
                if (r.Frequency > maxF) maxF = r.Frequency;
            }

            int count = readings.Count;

            var summaries = new List<NodeSummaryDto>();
            foreach (var kvp in GridDataRepository.Nodes)
            {
                summaries.Add(new NodeSummaryDto
                {
                    NodeId = kvp.Key,
                    NodeName = kvp.Value.Name,
                    City = kvp.Value.City,
                    AvgPower = Math.Round(sumP / count, 4),
                    AvgFrequency = Math.Round(sumF / count, 4),
                    FaultCount = faultCount
                });
            }

            return new StabilityReportDto
            {
                TotalReadings = count,
                FaultCount = faultCount,
                NormalCount = normalCount,
                WarningCount = warningCount,
                AvgVoltage = Math.Round(sumV / count, 2),
                AvgCurrent = Math.Round(sumA / count, 2),
                AvgPower = Math.Round(sumP / count, 4),
                AvgFrequency = Math.Round(sumF / count, 4),
                MaxPower = Math.Round(maxP, 4),
                MinFrequency = Math.Round(minF, 4),
                MaxFrequency = Math.Round(maxF, 4),
                FrequencyAnomalyCount = freqAnomalies.Count,
                PowerOverloadCount = powerAnomalies.Count,
                GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                NodeSummaries = summaries
            };
        }

        public List<AnomalyResultDto> DetectFrequencyAnomalies(string threshold)
        {
            double t = ParseDouble(threshold, 1.0);
            _freqDetector.FrequencyThreshold = t;
            return _freqDetector.Detect(_repo.GetAllReadings());
        }

        public List<AnomalyResultDto> DetectPowerOverloads(string threshold)
        {
            double t = ParseDouble(threshold, 4000.0);
            _powerDetector.PowerMaxThreshold = t;
            return _powerDetector.Detect(_repo.GetAllReadings());
        }

        public List<AnomalyResultDto> DetectZScoreAnomalies(string threshold)
        {
            double t = ParseDouble(threshold, 2.0);
            var readings = _repo.GetAllReadings();

            var anomalies = new List<AnomalyResultDto>();
            anomalies.AddRange(_freqDetector.DetectZScoreAnomalies(readings, t));
            anomalies.AddRange(_powerDetector.DetectZScoreAnomalies(readings, t));
            return anomalies;
        }

        public List<NodeStatusDto> GetAllNodeStatuses()
        {
            var batch = _repo.GetNextBatch(1);
            if (batch.Count == 0) return new List<NodeStatusDto>();

            var current = batch[0];
            var statuses = new List<NodeStatusDto>();

            foreach (var kvp in GridDataRepository.Nodes)
            {
                var nodeReading = _repo.GetNodeReading(current, kvp.Key);
                string status;
                if (nodeReading.FaultIndicator == 1) status = "Critical";
                else if (nodeReading.FaultIndicator == 2) status = "Warning";
                else status = "Normal";

                statuses.Add(new NodeStatusDto
                {
                    NodeId = kvp.Key,
                    NodeName = kvp.Value.Name,
                    City = kvp.Value.City,
                    Latitude = kvp.Value.Latitude,
                    Longitude = kvp.Value.Longitude,
                    Role = kvp.Value.Role,
                    Voltage = nodeReading.Voltage,
                    Current = nodeReading.Current,
                    Power = nodeReading.CalculatedPower,
                    Frequency = nodeReading.Frequency,
                    FaultIndicator = nodeReading.FaultIndicator,
                    Status = status,
                    LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
                });
            }

            return statuses;
        }

        public NodeStatusDto GetNodeStatus(string nodeId)
        {
            var batch = _repo.GetNextBatch(1);
            if (batch.Count == 0) return null;

            var nodeReading = _repo.GetNodeReading(batch[0], nodeId);
            var nodeInfo = GridDataRepository.Nodes[nodeId];

            string status;
            if (nodeReading.FaultIndicator == 1) status = "Critical";
            else if (nodeReading.FaultIndicator == 2) status = "Warning";
            else status = "Normal";

            return new NodeStatusDto
            {
                NodeId = nodeId,
                NodeName = nodeInfo.Name,
                City = nodeInfo.City,
                Latitude = nodeInfo.Latitude,
                Longitude = nodeInfo.Longitude,
                Role = nodeInfo.Role,
                Voltage = nodeReading.Voltage,
                Current = nodeReading.Current,
                Power = nodeReading.CalculatedPower,
                Frequency = nodeReading.Frequency,
                FaultIndicator = nodeReading.FaultIndicator,
                Status = status,
                LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
            };
        }

        public ConsumptionForecastDto GetConsumptionForecast(string nodeId, string points)
        {
            int p = ParseInt(points, 20);
            return _predictor.Predict(_repo.GetAllReadings(), nodeId, p);
        }

        public List<NotificationDto> GetRecentNotifications(string count)
        {
            int c = ParseInt(count, 50);
            return _eventManager.GetRecentNotifications(c);
        }

        public byte[] ExportCompressedData()
        {
            return _compressor.Compress(_repo.GetAllReadings());
        }

        // WCF WebGet parametri dolaze kao string, parsiramo rucno
        private static int ParseInt(string value, int defaultValue)
        {
            int result;
            if (int.TryParse(value, out result)) return result;
            return defaultValue;
        }

        private static double ParseDouble(string value, double defaultValue)
        {
            double result;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                return result;
            return defaultValue;
        }
    }
}