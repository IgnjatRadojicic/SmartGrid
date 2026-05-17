using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Contracts.DTOs;

namespace Client
{
    class Program
    {
        private const string BaseUrl = "http://localhost:5000";

        static void Main(string[] args)
        {
            Console.Title = "Smart Grid WCF Client";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==========================================");
            Console.WriteLine("  Smart Grid WCF Test Client");
            Console.WriteLine("==========================================");
            Console.ResetColor();
            Console.WriteLine();

            bool running = true;
            while (running)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Izaberi operaciju:");
                Console.ResetColor();
                Console.WriteLine("  1. Readings (paginacija)");
                Console.WriteLine("  2. Stability Report");
                Console.WriteLine("  3. Frequency Anomalies");
                Console.WriteLine("  4. Power Overloads");
                Console.WriteLine("  5. Z-Score Anomalies");
                Console.WriteLine("  6. Node Statuses");
                Console.WriteLine("  7. Node Detail");
                Console.WriteLine("  8. Consumption Forecast");
                Console.WriteLine("  9. Notifications");
                Console.WriteLine(" 10. Weather (svi gradovi)");
                Console.WriteLine(" 11. Weather Correlation");
                Console.WriteLine(" 12. Export Compressed Data");
                Console.WriteLine("  0. Izlaz");
                Console.Write("> ");

                string choice = Console.ReadLine();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1": TestReadings(); break;
                        case "2": TestReport(); break;
                        case "3": TestFrequencyAnomalies(); break;
                        case "4": TestPowerOverloads(); break;
                        case "5": TestZScoreAnomalies(); break;
                        case "6": TestNodeStatuses(); break;
                        case "7": TestNodeDetail(); break;
                        case "8": TestForecast(); break;
                        case "9": TestNotifications(); break;
                        case "10": TestWeather(); break;
                        case "11": TestWeatherCorrelation(); break;
                        case "12": TestExport(); break;
                        case "0": running = false; break;
                        default:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Nepoznata opcija.");
                            Console.ResetColor();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("GRESKA: " + ex.Message);
                    Console.ResetColor();
                }

                Console.WriteLine();
            }
        }

        // ==================== TEST METODE ====================

        static void TestReadings()
        {
            Console.Write("Stranica (default 1): ");
            string page = Console.ReadLine();
            if (string.IsNullOrEmpty(page)) page = "1";

            Console.Write("Velicina stranice (default 5): ");
            string size = Console.ReadLine();
            if (string.IsNullOrEmpty(size)) size = "5";

            string json = HttpGet("/api/grid/readings?page=" + page + "&pageSize=" + size);
            var readings = Deserialize<List<GridReadingDto>>(json);

            if (readings == null || readings.Count == 0)
            {
                PrintWarning("Nema podataka.");
                return;
            }

            PrintHeader("READINGS (stranica " + page + ")");
            foreach (var r in readings)
            {
                Console.WriteLine(string.Format(
                    "  [{0}] {1} | V={2:F1}V  I={3:F1}A  P={4:F2}kW  f={5:F2}Hz  Fault={6}",
                    r.Id, r.TimestampFormatted, r.Voltage, r.Current,
                    r.PowerUsage, r.Frequency, r.FaultIndicator));
            }
            PrintCount(readings.Count);
        }

        static void TestReport()
        {
            string json = HttpGet("/api/grid/report");
            var report = Deserialize<StabilityReportDto>(json);

            if (report == null) { PrintWarning("Nema izvestaja."); return; }

            PrintHeader("STABILITY REPORT");
            Console.WriteLine("  Ukupno zapisa:     " + report.TotalReadings);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  Normalni:          " + report.NormalCount);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Upozorenja:        " + report.WarningCount);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  Kvarovi:           " + report.FaultCount);
            Console.ResetColor();
            Console.WriteLine("  Avg Voltage:       " + report.AvgVoltage + " V");
            Console.WriteLine("  Avg Current:       " + report.AvgCurrent + " A");
            Console.WriteLine("  Avg Power:         " + report.AvgPower + " kW");
            Console.WriteLine("  Avg Frequency:     " + report.AvgFrequency + " Hz");
            Console.WriteLine("  Max Power:         " + report.MaxPower + " kW");
            Console.WriteLine("  Freq Range:        " + report.MinFrequency + " - " + report.MaxFrequency + " Hz");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  Freq Anomalies:    " + report.FrequencyAnomalyCount);
            Console.WriteLine("  Power Overloads:   " + report.PowerOverloadCount);
            Console.ResetColor();

            if (report.NodeSummaries != null)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("  Node Summaries:");
                Console.ResetColor();
                foreach (var ns in report.NodeSummaries)
                {
                    Console.WriteLine(string.Format("    {0} ({1}): AvgP={2}kW  AvgF={3}Hz  Faults={4}",
                        ns.NodeName, ns.City, ns.AvgPower, ns.AvgFrequency, ns.FaultCount));
                }
            }
        }

        static void TestFrequencyAnomalies()
        {
            Console.Write("Prag (default 1.0 Hz): ");
            string threshold = Console.ReadLine();
            if (string.IsNullOrEmpty(threshold)) threshold = "1.0";

            string json = HttpGet("/api/grid/anomalies/frequency?threshold=" + threshold);
            var anomalies = Deserialize<List<AnomalyResultDto>>(json);

            PrintHeader("FREQUENCY ANOMALIES (prag: " + threshold + " Hz)");
            PrintAnomalies(anomalies);
        }

        static void TestPowerOverloads()
        {
            Console.Write("Prag (default 4000 W): ");
            string threshold = Console.ReadLine();
            if (string.IsNullOrEmpty(threshold)) threshold = "4000";

            string json = HttpGet("/api/grid/anomalies/power?threshold=" + threshold);
            var anomalies = Deserialize<List<AnomalyResultDto>>(json);

            PrintHeader("POWER OVERLOADS (prag: " + threshold + " W)");
            PrintAnomalies(anomalies);
        }

        static void TestZScoreAnomalies()
        {
            Console.Write("Z-Score prag (default 2.0): ");
            string threshold = Console.ReadLine();
            if (string.IsNullOrEmpty(threshold)) threshold = "2.0";

            string json = HttpGet("/api/grid/anomalies/zscore?threshold=" + threshold);
            var anomalies = Deserialize<List<AnomalyResultDto>>(json);

            PrintHeader("Z-SCORE ANOMALIES (prag: " + threshold + ")");
            PrintAnomalies(anomalies);
        }

        static void TestNodeStatuses()
        {
            string json = HttpGet("/api/grid/nodes");
            var nodes = Deserialize<List<NodeStatusDto>>(json);

            if (nodes == null || nodes.Count == 0) { PrintWarning("Nema node-ova."); return; }

            PrintHeader("NODE STATUSES");
            foreach (var n in nodes)
            {
                ConsoleColor color = ConsoleColor.Green;
                if (n.Status == "Warning") color = ConsoleColor.Yellow;
                if (n.Status == "Critical") color = ConsoleColor.Red;

                Console.ForegroundColor = color;
                Console.Write("  [" + n.Status.PadRight(8) + "] ");
                Console.ResetColor();
                Console.WriteLine(string.Format("{0} ({1}) | V={2}V I={3}A P={4}W f={5}Hz | {6}",
                    n.NodeName, n.City, n.Voltage, n.Current,
                    n.Power, n.Frequency, n.Role));
            }
        }

        static void TestNodeDetail()
        {
            Console.Write("Node ID (Node1/Node2/Node3/Node4): ");
            string nodeId = Console.ReadLine();
            if (string.IsNullOrEmpty(nodeId)) nodeId = "Node1";

            string json = HttpGet("/api/grid/nodes/" + nodeId);
            var node = Deserialize<NodeStatusDto>(json);

            if (node == null) { PrintWarning("Node nije pronadjen."); return; }

            PrintHeader("NODE: " + node.NodeName);
            Console.WriteLine("  Grad:        " + node.City);
            Console.WriteLine("  Uloga:       " + node.Role);
            Console.WriteLine("  Koordinate:  " + node.Latitude + ", " + node.Longitude);
            Console.WriteLine("  Voltage:     " + node.Voltage + " V");
            Console.WriteLine("  Current:     " + node.Current + " A");
            Console.WriteLine("  Power:       " + node.Power + " W");
            Console.WriteLine("  Frequency:   " + node.Frequency + " Hz");
            Console.WriteLine("  Fault:       " + node.FaultIndicator);

            ConsoleColor color = ConsoleColor.Green;
            if (node.Status == "Warning") color = ConsoleColor.Yellow;
            if (node.Status == "Critical") color = ConsoleColor.Red;
            Console.ForegroundColor = color;
            Console.WriteLine("  Status:      " + node.Status);
            Console.ResetColor();
        }

        static void TestForecast()
        {
            Console.Write("Node ID (default Node2): ");
            string nodeId = Console.ReadLine();
            if (string.IsNullOrEmpty(nodeId)) nodeId = "Node2";

            Console.Write("Forecast tacaka (default 10): ");
            string points = Console.ReadLine();
            if (string.IsNullOrEmpty(points)) points = "10";

            string json = HttpGet("/api/grid/forecast/" + nodeId + "?points=" + points);
            var forecast = Deserialize<ConsumptionForecastDto>(json);

            if (forecast == null || forecast.ForecastPoints == null)
            {
                PrintWarning("Nema forecast podataka.");
                return;
            }

            PrintHeader("FORECAST za " + nodeId);
            foreach (var fp in forecast.ForecastPoints)
            {
                int barLen = (int)(fp.Confidence * 20);
                string bar = new string('#', barLen) + new string('.', 20 - barLen);

                Console.WriteLine(string.Format("  {0}  P={1:F3}kW  [{2}] {3:F0}%",
                    fp.Timestamp, fp.PredictedPower, bar, fp.Confidence * 100));
            }
        }

        static void TestNotifications()
        {
            string json = HttpGet("/api/grid/notifications?count=20");
            var notifications = Deserialize<List<NotificationDto>>(json);

            if (notifications == null || notifications.Count == 0)
            {
                PrintWarning("Nema notifikacija.");
                return;
            }

            PrintHeader("NOTIFICATIONS (poslednjih 20)");
            foreach (var n in notifications)
            {
                ConsoleColor color = ConsoleColor.Gray;
                if (n.Severity == "High" || n.Severity == "Critical") color = ConsoleColor.Red;
                else if (n.Severity == "Medium") color = ConsoleColor.Yellow;
                else if (n.Severity == "Low") color = ConsoleColor.Green;

                Console.ForegroundColor = color;
                Console.Write("  [" + n.Severity.PadRight(8) + "] ");
                Console.ResetColor();
                Console.WriteLine(n.Timestamp + " | " + n.Message);
            }
            PrintCount(notifications.Count);
        }

        static void TestWeather()
        {
            string json = HttpGet("/api/weather/all");
            var weatherList = Deserialize<List<WeatherDataDto>>(json);

            if (weatherList == null || weatherList.Count == 0)
            {
                PrintWarning("Nema vremenskih podataka.");
                return;
            }

            PrintHeader("WEATHER (Srbija)");
            foreach (var w in weatherList)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("  " + w.City.PadRight(15));
                Console.ResetColor();
                Console.WriteLine(string.Format("{0:F1}°C  | Vlaznost: {1}%  | Vetar: {2} m/s  | {3}",
                    w.Temperature, w.Humidity, w.WindSpeed, w.Description));
            }
        }

        static void TestWeatherCorrelation()
        {
            string json = HttpGet("/api/weather/correlation");
            var correlation = Deserialize<WeatherCorrelationDto>(json);

            if (correlation == null || correlation.NodeCorrelations == null)
            {
                PrintWarning("Nema korelacionih podataka.");
                return;
            }

            PrintHeader("WEATHER CORRELATION");
            foreach (var nc in correlation.NodeCorrelations)
            {
                ConsoleColor color = ConsoleColor.Green;
                if (nc.RiskLevel == "Medium") color = ConsoleColor.Yellow;
                if (nc.RiskLevel == "High") color = ConsoleColor.Red;

                Console.ForegroundColor = color;
                Console.Write("  [" + nc.RiskLevel.PadRight(6) + "] ");
                Console.ResetColor();
                Console.WriteLine(string.Format("{0}: {1:F1}°C  Risk: x{2}  EstPower: {3}W",
                    nc.City, nc.Temperature, nc.RiskFactor, nc.EstimatedPowerUsage));
            }
        }

        static void TestExport()
        {
            PrintHeader("EXPORT COMPRESSED DATA");
            Console.WriteLine("  Downloading compressed data...");

            string json = HttpGet("/api/grid/export");
            if (string.IsNullOrEmpty(json))
            {
                PrintWarning("Export failed.");
                return;
            }

            // Response je base64 encoded byte array u JSON
            byte[] compressed = Deserialize<byte[]>(json);
            if (compressed == null)
            {
                PrintWarning("Deserijalizacija failed.");
                return;
            }

            string outputPath = "grid_export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".gz";
            File.WriteAllBytes(outputPath, compressed);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  Sacuvano: " + outputPath);
            Console.WriteLine("  Velicina: " + compressed.Length + " bytes");
            Console.ResetColor();
        }

        // ==================== HTTP HELPER ====================

        static string HttpGet(string path)
        {
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                return client.DownloadString(BaseUrl + path);
            }
        }

        // ==================== JSON DESERIJALIZACIJA ====================

        static T Deserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default(T);

            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (T)serializer.ReadObject(stream);
            }
        }

        // ==================== PRINT HELPERS ====================

        static void PrintHeader(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("--- " + title + " ---");
            Console.ResetColor();
        }

        static void PrintWarning(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  " + msg);
            Console.ResetColor();
        }

        static void PrintCount(int count)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  (" + count + " stavki)");
            Console.ResetColor();
        }
        static void PrintAnomalies(List<AnomalyResultDto> anomalies)
        {
            if (anomalies == null || anomalies.Count == 0)
            {
                PrintWarning("Nema anomalija.");
                return;
            }

            foreach (var a in anomalies)
            {
                ConsoleColor color = ConsoleColor.Green;
                if (a.Severity == "High" || a.Severity == "Critical") color = ConsoleColor.Red;
                else if (a.Severity == "Medium") color = ConsoleColor.Yellow;

                Console.ForegroundColor = color;
                Console.Write("  [" + a.Severity.PadRight(8) + "] ");
                Console.ResetColor();
                Console.WriteLine(string.Format("{0} | {1} | Val={2} | {3}",
                    a.DetectedAt, a.AnomalyType, a.Value, a.Description));
            }
            PrintCount(anomalies.Count);
        }
    }
}