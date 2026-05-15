using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Contracts.DTOs;

namespace Data.Readers
{
    // Cita Smart Grid CSV sa Dispose patternom
    // Stream chain: FileStream -> BufferedStream -> StreamReader
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
                throw new FileNotFoundException("CSV fajl nije pronadjen: " + filePath);

            _filePath = filePath;
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _bufferedStream = new BufferedStream(_fileStream, 8192);
            _reader = new StreamReader(_bufferedStream);
        }

        // Ucitava sve redove odjednom u memoriju
        public List<GridReadingDto> ReadAll()
        {
            ThrowIfDisposed();

            var readings = new List<GridReadingDto>();
            int id = 0;

            string headerLine = _reader.ReadLine();
            if (headerLine == null)
                throw new InvalidDataException("CSV fajl je prazan.");

            string line;
            while ((line = _reader.ReadLine()) != null)
            {
                var reading = ParseLine(line, id);
                if (reading != null)
                {
                    readings.Add(reading);
                    id++;
                }
            }

            return readings;
        }

        // Lazy loading - yield return, jedan red u memoriji
        public IEnumerable<GridReadingDto> ReadBatched()
        {
            ThrowIfDisposed();

            _reader.ReadLine(); // preskoci header
            int id = 0;
            string line;

            while ((line = _reader.ReadLine()) != null)
            {
                var reading = ParseLine(line, id);
                if (reading != null)
                {
                    yield return reading;
                    id++;
                }
            }
        }

        // Format: Timestamp,Voltage,Current,PowerUsage,Frequency,FaultIndicator,FFT_1..FFT_128
        private GridReadingDto ParseLine(string line, int id)
        {
            try
            {
                var parts = line.Split(',');
                if (parts.Length < 6)
                    return null;

                var voltage = double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
                var current = double.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
                var timestamp = DateTime.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);

                // FFT koeficijenti (128 kolona, indeksi 6-133)
                var fft = new double[128];
                for (int i = 0; i < 128 && (i + 6) < parts.Length; i++)
                {
                    double.TryParse(parts[i + 6].Trim(), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out fft[i]);
                }

                return new GridReadingDto
                {
                    Id = id,
                    Timestamp = timestamp,
                    TimestampFormatted = timestamp.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Voltage = voltage,
                    Current = current,
                    PowerUsage = double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                    Frequency = double.Parse(parts[4].Trim(), CultureInfo.InvariantCulture),
                    FaultIndicator = int.Parse(parts[5].Trim()),
                    FFT = fft,
                    CalculatedPower = voltage * current  // P(t) = V(t) * I(t)
                };
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CsvDataReader));
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
                // Managed resursi - obrnut redosled od kreiranja
                if (_reader != null) { _reader.Dispose(); _reader = null; }
                if (_bufferedStream != null) { _bufferedStream.Dispose(); _bufferedStream = null; }
                if (_fileStream != null) { _fileStream.Dispose(); _fileStream = null; }
            }

            _disposed = true;
        }

        ~CsvDataReader()
        {
            Dispose(false);
        }
    }
}