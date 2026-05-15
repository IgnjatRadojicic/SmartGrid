using SmartGrid.Contracts.DTOs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection.PortableExecutable;

namespace SmartGrid.Data.Readers
{
    public class CsvDataReader : IDisposable
    {
        private FileStream _fileStream;
        private BufferedStream _bufferedStream;
        private StreamReader _reader;
        private bool _disposed = false;
        private readonly string _filePath;

        public CsvDataReader(string filePath)
        {

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"CSV fajl nije pronađen: {filePath}");

            _filePath = filePath;

            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _bufferedStream = new BufferedStream(_fileStream, bufferSize: 8192);
            _reader = new StreamReader(_bufferedStream);
        }
        /// <summary>
        /// Cita CSV u batch-evima (lazy loading za velike fajlove)
        /// Koristi yield return ne ucitava ceo fajl u memoriju odjednom
        /// </summary>
        /// 
        public List<GridReadingDto> ReadAll()
        {
            ThrowIfDisposed();

            var readings = new List<GridReadingDto>();
            var baseTime = DateTime.UtcNow;
            int id = 0;

            // Preskoči header red
            string headerLine = _reader.ReadLine();
            if (headerLine == null)
                throw new InvalidDataException("CSV fajl je prazan.");

            string line;
            while ((line = _reader.ReadLine()) != null)
            {
                var reading = ParseLine(line, id, baseTime.AddSeconds(id * 3));
                if (reading != null)
                {
                    readings.Add(reading);
                    id++;
                }
            }

            return readings;
        }

        /// <summary>
        /// Cita CSV u batch-evima (lazy loading za velike fajlove)
        /// Koristi yield return ne ucitava ceo fajl u memoriju odjednom
        /// </summary>
        /// 
        public IEnumerable<GridReadingDto> ReadBatched(int batchSize = 1000)
        {
            ThrowIfDisposed();

            var baseTime = DateTime.UtcNow;
            int id = 0;

            _reader.ReadLine();

            string line;
            while((line = _reader.ReadLine()) != null)
            {
                var reading = ParseLine(line, id, baseTime.AddSeconds(id * 3));
                if (reading != null)
                {
                    yield return reading;
                    id++;
                }
            }
        }


        /// <summary>
        /// Parsira jedan red CSV-a u GridReadingDto.
        /// Format: tau1,tau2,tau3,tau4,p1,p2,p3,p4,g1,g2,g3,g4,stab,stabf
        /// </summary>
        private GridReadingDto ParseLine(string line, int id, DateTime timestamp)
        {
            try
            {
                var parts = line.Split(',');
                if (parts.Length < 14)
                    return null;

                return new GridReadingDto
                {
                    Id = id,
                    Tau1 = double.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                    Tau2 = double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                    Tau3 = double.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                    Tau4 = double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                    P1 = double.Parse(parts[4].Trim(), CultureInfo.InvariantCulture),
                    P2 = double.Parse(parts[5].Trim(), CultureInfo.InvariantCulture),
                    P3 = double.Parse(parts[6].Trim(), CultureInfo.InvariantCulture),
                    P4 = double.Parse(parts[7].Trim(), CultureInfo.InvariantCulture),
                    G1 = double.Parse(parts[8].Trim(), CultureInfo.InvariantCulture),
                    G2 = double.Parse(parts[9].Trim(), CultureInfo.InvariantCulture),
                    G3 = double.Parse(parts[10].Trim(), CultureInfo.InvariantCulture),
                    G4 = double.Parse(parts[11].Trim(), CultureInfo.InvariantCulture),
                    Stab = double.Parse(parts[12].Trim(), CultureInfo.InvariantCulture),
                    StabF = parts[13].Trim(),
                    Timestamp = timestamp
                };
            }
            catch (FormatException)
            {
                return null;
            }
        }
        private void ThrowIfDisposed()
        {
            if (!_disposed)
                throw new ObjectDisposedException(nameof(CsvDataReader));
        }

        /// <summary>
        /// Javni Dispose poziva ga korisnik ili using blok
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Zasticeni dispose cisti managed i unmanaged resurse,
        /// disposing = true -> pozvan iz Dispose() cisti sve
        /// disposing = false -> pozvan iz finalizera cisti samo unmanaged
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            if (disposing)
            {
                _reader?.Dispose();
                _bufferedStream?.Dispose();
                _fileStream?.Dispose();

                _reader = null;
                _bufferedStream = null;
                _fileStream = null;
            }
        }

        /// <summary>
        /// Finalizer safety net ako korisnik zaboravi Dispose().
        /// </summary>
        ~CsvDataReader()
        {
            Dispose(disposing: false);
        }
    }

}