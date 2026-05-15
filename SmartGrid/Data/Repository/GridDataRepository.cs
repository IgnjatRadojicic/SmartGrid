using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.DTOs;
using Data.Readers;

namespace Data.Repository
{
    // Centralno skladiste podataka. Drzi readings u memoriji,
    // simulira real-time feed sa kursorom, i mapira node-ove na gradove.
    public class GridDataRepository : IDisposable
    {
        private List<GridReadingDto> _allReadings;
        private int _currentIndex = 0;
        private bool _disposed = false;
        private readonly Random _rng = new Random(42);

        // Node definicije - mapiranje na srpske gradove
        public static readonly Dictionary<string, NodeInfo> Nodes = new Dictionary<string, NodeInfo>
        {
            { "Node1", new NodeInfo("Node1", "TE Nikola Tesla", "Obrenovac", 44.6167, 20.2000, "Supplier") },
            { "Node2", new NodeInfo("Node2", "Potrosac Beograd", "Belgrade", 44.8176, 20.4569, "Consumer") },
            { "Node3", new NodeInfo("Node3", "Potrosac Novi Sad", "Novi Sad", 45.2671, 19.8335, "Consumer") },
            { "Node4", new NodeInfo("Node4", "Potrosac Nis", "Nis", 43.3209, 21.8958, "Consumer") },
        };

        // Mnozitelji za simulaciju 4 node-a iz jednog dataseta
        private static readonly Dictionary<string, double> NodeMultipliers = new Dictionary<string, double>
        {
            { "Node1", 1.0 },     // originalni podaci (proizvodi)
            { "Node2", 1.1 },     // Beograd - najveci potrosac
            { "Node3", 0.85 },    // Novi Sad - srednji
            { "Node4", 0.7 },     // Nis - najmanji
        };

        public void LoadFromCsv(string filePath)
        {
            ThrowIfDisposed();
            using (var reader = new CsvDataReader(filePath))
            {
                _allReadings = reader.ReadAll();
            }
        }

        public List<GridReadingDto> GetAllReadings()
        {
            ThrowIfDisposed();
            return _allReadings ?? new List<GridReadingDto>();
        }

        public List<GridReadingDto> GetReadings(int page, int pageSize)
        {
            ThrowIfDisposed();
            return (_allReadings ?? new List<GridReadingDto>())
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public GridReadingDto GetReadingById(int id)
        {
            ThrowIfDisposed();
            if (_allReadings == null) return null;
            return _allReadings.FirstOrDefault(r => r.Id == id);
        }

        // Simulira real-time: svaki poziv vraca sledeci batch.
        // Circular buffer - kad dodje do kraja vraca se na pocetak.
        public List<GridReadingDto> GetNextBatch(int count = 10)
        {
            ThrowIfDisposed();
            if (_allReadings == null || _allReadings.Count == 0)
                return new List<GridReadingDto>();

            var batch = new List<GridReadingDto>();
            for (int i = 0; i < count; i++)
            {
                var idx = _currentIndex % _allReadings.Count;
                batch.Add(_allReadings[idx]);
                _currentIndex++;
            }
            return batch;
        }

        // Vraca podatke prilagodjene za specifican node.
        // Mnoziocima simuliramo razlicitu potrosnju po gradu.
        public GridReadingDto GetNodeReading(GridReadingDto original, string nodeId)
        {
            ThrowIfDisposed();
            double mult = NodeMultipliers.ContainsKey(nodeId) ? NodeMultipliers[nodeId] : 1.0;
            double noise = (_rng.NextDouble() - 0.5) * 0.02; // +/- 1% noise

            return new GridReadingDto
            {
                Id = original.Id,
                Timestamp = original.Timestamp,
                TimestampFormatted = original.TimestampFormatted,
                Voltage = Math.Round(original.Voltage * (mult + noise), 2),
                Current = Math.Round(original.Current * (mult + noise), 2),
                PowerUsage = Math.Round(original.PowerUsage * mult, 4),
                Frequency = Math.Round(original.Frequency + noise * 0.5, 4),
                FaultIndicator = original.FaultIndicator,
                FFT = original.FFT,
                CalculatedPower = Math.Round(original.Voltage * mult * original.Current * mult, 4)
            };
        }

        public void AddReadings(List<GridReadingDto> newReadings)
        {
            ThrowIfDisposed();
            if (_allReadings == null) _allReadings = new List<GridReadingDto>();
            _allReadings.AddRange(newReadings);
        }

        public int TotalCount { get { return _allReadings != null ? _allReadings.Count : 0; } }
        public int CurrentPosition { get { return _currentIndex; } }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GridDataRepository));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                if (_allReadings != null) { _allReadings.Clear(); _allReadings = null; }
            }
            _disposed = true;
        }

        ~GridDataRepository()
        {
            Dispose(false);
        }
    }

    public class NodeInfo
    {
        public string NodeId { get; private set; }
        public string Name { get; private set; }
        public string City { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public string Role { get; private set; }

        public NodeInfo(string nodeId, string name, string city, double lat, double lon, string role)
        {
            NodeId = nodeId;
            Name = name;
            City = city;
            Latitude = lat;
            Longitude = lon;
            Role = role;
        }
    }
}