using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Web;
using Core.Analysis;
using Core.Events;
using Core.ExternalApi;
using Data.Repository;
using Data.Streams;
using Data.Watchers;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            // Ucitaj konfiguraciju
            string csvPath = ConfigurationManager.AppSettings["CsvPath"];
            string watchPath = ConfigurationManager.AppSettings["WatchPath"];
            string archivePath = ConfigurationManager.AppSettings["ArchivePath"];
            string weatherApiKey = ConfigurationManager.AppSettings["OpenWeatherApiKey"];

            // Inicijalizuj komponente
            var eventManager = new GridEventManager();
            var repo = new GridDataRepository();
            var freqDetector = new FrequencyChangeDetector(eventManager, 1.0);
            var powerDetector = new PowerOverloadDetector(eventManager, 4000.0);
            var predictor = new ConsumptionPredictor(50);
            var compressor = new DataCompressor();
            var weatherClient = new OpenWeatherClient(weatherApiKey);

            // Ucitaj CSV
            Console.WriteLine("[Startup] Ucitavam dataset: " + csvPath);
            repo.LoadFromCsv(csvPath);
            Console.WriteLine("[Startup] Ucitano " + repo.TotalCount + " zapisa.");

            // Pokreni FileWatcher
            var fileWatcher = new GridFileWatcher(watchPath);
            fileWatcher.FileDetected += (sender, e) =>
            {
                Console.WriteLine("[FileWatcher] Obradjujem: " + e.FileName);
                try
                {
                    using (var reader = new Data.Readers.CsvDataReader(e.FullPath))
                    {
                        var newReadings = reader.ReadAll();
                        repo.AddReadings(newReadings);
                        Console.WriteLine("[FileWatcher] Dodato " + newReadings.Count + " novih zapisa.");
                    }

                    // Arhiviraj
                    var transfer = new StreamTransfer();
                    transfer.ArchiveFile(e.FullPath, archivePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[FileWatcher] Greska: " + ex.Message);
                }
            };
            fileWatcher.Start();

            // Pretplati se na evente za konzolni ispis
            eventManager.AnomalyDetected += (sender, e) =>
            {
                Console.WriteLine(string.Format("[EVENT] {0} | {1} | {2}",
                    e.Anomaly.Severity, e.Anomaly.AnomalyType, e.Anomaly.Description));
            };

            eventManager.ThresholdExceeded += (sender, e) =>
            {
                Console.WriteLine(string.Format("[THRESHOLD] {0} na {1} = {2:F2}",
                    e.MetricName, e.NodeId, e.Value));
            };

            eventManager.FaultDetected += (sender, e) =>
            {
                Console.WriteLine(string.Format("[FAULT] {0} na {1}: {2}",
                    e.FaultType, e.NodeName, e.Description));
            };

            // Inicijalizuj servise
            GridAnalysisService.Initialize(repo, eventManager, freqDetector,
                powerDetector, predictor, compressor);
            WeatherService.Initialize(weatherClient, powerDetector, repo);

            // Pokreni WCF hostove
            var gridHost = new WebServiceHost(typeof(GridAnalysisService));
            var weatherHost = new WebServiceHost(typeof(WeatherService));

            var corsBehavior = new CorsBehavior();
            foreach (var endpoint in gridHost.Description.Endpoints)
                endpoint.EndpointBehaviors.Add(corsBehavior);
            foreach (var endpoint in weatherHost.Description.Endpoints)
                endpoint.EndpointBehaviors.Add(corsBehavior);


            try
            {
                gridHost.Open();
                weatherHost.Open();

                Console.WriteLine("");
                Console.WriteLine("==========================================");
                Console.WriteLine("  Smart Grid WCF Service");
                Console.WriteLine("==========================================");
                Console.WriteLine("  Grid API:    http://localhost:5000/api/grid");
                Console.WriteLine("  Weather API: http://localhost:5000/api/weather");
                Console.WriteLine("==========================================");
                Console.WriteLine("");
                Console.WriteLine("Endpoints:");
                Console.WriteLine("  GET /api/grid/readings?page=1&pageSize=50");
                Console.WriteLine("  GET /api/grid/report");
                Console.WriteLine("  GET /api/grid/anomalies/frequency?threshold=1.0");
                Console.WriteLine("  GET /api/grid/anomalies/power?threshold=4000");
                Console.WriteLine("  GET /api/grid/anomalies/zscore?threshold=2.0");
                Console.WriteLine("  GET /api/grid/nodes");
                Console.WriteLine("  GET /api/grid/nodes/Node1");
                Console.WriteLine("  GET /api/grid/forecast/Node1?points=20");
                Console.WriteLine("  GET /api/grid/notifications?count=50");
                Console.WriteLine("  GET /api/grid/export");
                Console.WriteLine("  GET /api/weather/current/Belgrade");
                Console.WriteLine("  GET /api/weather/all");
                Console.WriteLine("  GET /api/weather/correlation");
                Console.WriteLine("");
                Console.WriteLine("Pritisni Enter za zaustavljanje...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] " + ex.Message);
                Console.ReadLine();
            }
            finally
            {
                gridHost.Close();
                weatherHost.Close();
                fileWatcher.Stop();
                fileWatcher.Dispose();
                repo.Dispose();
                compressor.Dispose();
            }
        }
    }
}