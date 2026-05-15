using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using SmartGrid.Contracts.DTOs;

namespace SmartGrid.Data.Streams
{

    public class DataCompressor : IDisposable
    {
        private bool _disposed = false;

        /// <summary>
        /// Kompresuje listu readings u GZip byte array.
        /// </summary>
        public byte[] Compress(List<GridReadingDto> readings)
        {
            ThrowIfDisposed();

            // 1. Serijalizuj u JSON
            string json = JsonSerializer.Serialize(readings);
            byte[] rawBytes = Encoding.UTF8.GetBytes(json);

            // 2. Kompresuj kroz GZipStream
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
                {
                    gzipStream.Write(rawBytes, 0, rawBytes.Length);
                }

                byte[] compressed = outputStream.ToArray();

                Console.WriteLine($"[Compressor] Original: {rawBytes.Length} bytes → Compressed: {compressed.Length} bytes ({CompressionRatio(rawBytes.Length, compressed.Length):F1}% reduction)");

                return compressed;
            }
        }

        /// <summary>
        /// Dekompresuje GZip byte array nazad u listu readings.
        /// </summary>
        public List<GridReadingDto> Decompress(byte[] compressedData)
        {
            ThrowIfDisposed();

            using (var inputStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                byte[] rawBytes = outputStream.ToArray();
                string json = Encoding.UTF8.GetString(rawBytes);

                return JsonSerializer.Deserialize<List<GridReadingDto>>(json);
            }
        }

        /// <summary>
        /// Kompresuje CSV fajl direktno i sačuva kao .gz
        /// </summary>
        public void CompressFile(string inputPath, string outputPath)
        {
            ThrowIfDisposed();

            using (var inputFile = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
            using (var outputFile = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (var gzipStream = new GZipStream(outputFile, CompressionLevel.Optimal))
            {
                inputFile.CopyTo(gzipStream);
            }

            var originalSize = new FileInfo(inputPath).Length;
            var compressedSize = new FileInfo(outputPath).Length;
            Console.WriteLine($"[Compressor] File: {originalSize} {compressedSize} bytes ({CompressionRatio(originalSize, compressedSize):F1}% reduction)");
        }

        /// <summary>
        /// Dekompresuje .gz fajl nazad u original.
        /// </summary>
        public void DecompressFile(string gzipPath, string outputPath)
        {
            ThrowIfDisposed();

            using (var inputFile = new FileStream(gzipPath, FileMode.Open, FileAccess.Read))
            using (var gzipStream = new GZipStream(inputFile, CompressionMode.Decompress))
            using (var outputFile = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                gzipStream.CopyTo(outputFile);
            }
        }

        private double CompressionRatio(long original, long compressed)
        {
            return (1.0 - (double)compressed / original) * 100.0;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DataCompressor));
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;
        }

        ~DataCompressor()
        {
            Dispose(disposing: false);
        }
    }
}