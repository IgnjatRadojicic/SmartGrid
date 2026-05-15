using System;
using System.Collections.Generic;
using System.Linq;
using SmartGrid.Contracts.DTOs;
using SmartGrid.Data.Entities;
using SmartGrid.Data.Readers;

namespace SmartGrid.Data.Repository
{
    /// <summary>
    /// Centralno skladište podataka drzi učitane readings u memoriji
    /// i simulira real-time feed (pomera "kursor" svake 3 sekunde).
    /// </summary>
    public class GridDataRepository : IDisposable
    {
        private List<GridReadingDto> _allReadings;
        private int _currentIndex = 0;
        private bool _disposed = false;
        private CsvDataReader _reader;

        // Node definicije za mapiranje na gradove
        public static readonly Dictionary<string, NodeInfo> Nodes = new()
        {
            ["Node1"] = new NodeInfo("Node1", "TE Nikola Tesla", "Obrenovac", 44.6167, 20.2000, "Supplier"),
            ["Node2"] = new NodeInfo("Node2", "Potrošač Beograd", "Beograd", 44.8176, 20.4569, "Consumer"),
            ["Node3"] = new NodeInfo("Node3", "Potrošač Novi Sad", "Novi Sad", 45.2671, 19.8335, "Consumer"),
            ["Node4"] = new NodeInfo("Node4", "Potrošač Niš", "Niš", 43.3209, 21.8958, "Consumer"),
        };

        /// <summary>
        /// Ucitava CSV fajl u memoriju.
        /// </summary>
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
            return _allReadings?
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList() ?? new List<GridReadingDto>();
        }


        public GridReadingDto GetReadingById(int id)
        {
            ThrowIfDisposed();
            return _allReadings?.FirstOrDefault(r => r.Id == id);
        }


        public List<GridReadingDto> GetNextBatch(int count = 10)
        {
            ThrowIfDisposed();

            if (_allReadings == null || _allReadings.Count == 0)
                return new List<GridReadingDto>();

            var batch = new List<GridReadingDto>();
            for (int i = 0; i < count; i++)
            {
                var reading = _allReadings[_currentIndex % _allReadings.Count];

                // Azuriraj timestamp na "sada" za simulaciju real-time
                reading.Timestamp = DateTime.UtcNow.AddSeconds(-count + i);

                batch.Add(reading);
                _currentIndex++;
            }

            return batch;
        }

        /// <summary>
        /// Vraca podatke za specifican node (1-4).
        /// Node 1: tau1, p1, g1
        /// Node 2: tau2, p2, g2 itd.
        /// </summary>
        public List<(double Tau, double Power, double Gamma, double Stab, DateTime Time)>
            GetNodeHistory(string nodeId, int lastN = 100)
        {
            ThrowIfDisposed();

            var readings = _allReadings?
                .Skip(Math.Max(0, _currentIndex - lastN))
                .Take(lastN)
                .ToList() ?? new List<GridReadingDto>();

            return readings.Select(r => nodeId switch
            {
                "Node1" => (r.Tau1, r.P1, r.G1, r.Stab, r.Timestamp),
                "Node2" => (r.Tau2, r.P2, r.G2, r.Stab, r.Timestamp),
                "Node3" => (r.Tau3, r.P3, r.G3, r.Stab, r.Timestamp),
                "Node4" => (r.Tau4, r.P4, r.G4, r.Stab, r.Timestamp),
                _ => (0.0, 0.0, 0.0, 0.0, r.Timestamp)
            }).ToList();
        }

        /// <summary>
        /// Dodaje nove readings
        /// </summary>
        public void AddReadings(List<GridReadingDto> newReadings)
        {
            ThrowIfDisposed();
            _allReadings ??= new List<GridReadingDto>();
            _allReadings.AddRange(newReadings);
        }

        public int TotalCount => _allReadings?.Count ?? 0;
        public int CurrentPosition => _currentIndex;

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GridDataRepository));
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _allReadings?.Clear();
                _allReadings = null;
            }

            _disposed = true;
        }

        ~GridDataRepository()
        {
            Dispose(disposing: false);
        }
    }

}