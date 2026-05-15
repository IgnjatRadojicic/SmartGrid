using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Json;
using System.Text;
using Contracts.DTOs;

namespace Data.Streams
{
    // GZipStream kompresija za mrezni prenos preko WCF-a.
    // Pipeline: List -> JSON -> byte[] -> GZipStream -> kompresovani byte[]
    public class DataCompressor : IDisposable
    {
        private bool _disposed = false;

        // Kompresuje readings u GZip byte array za WCF transfer
        public byte[] Compress(List<GridReadingDto> readings)
        {
            ThrowIfDisposed();

            // Serijalizuj u JSON pomocu DataContractJsonSerializer (.NET 4.7.2)
            var serializer = new DataContractJsonSerializer(typeof(List<GridReadingDto>));
            byte[] rawBytes;

            using (var jsonStream = new MemoryStream())
            {
                serializer.WriteObject(jsonStream, readings);
                rawBytes = jsonStream.ToArray();
            }

            // Kompresuj
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, true))
                {
                    gzipStream.Write(rawBytes, 0, rawBytes.Length);
                }

                byte[] compressed = outputStream.ToArray();
                Console.WriteLine(string.Format("[Compressor] {0} bytes -> {1} bytes ({2:F1}% reduction)",
                    rawBytes.Length, compressed.Length, (1.0 - (double)compressed.Length / rawBytes.Length) * 100));
                return compressed;
            }
        }

        // Dekompresuje nazad u listu readings
        public List<GridReadingDto> Decompress(byte[] compressedData)
        {
            ThrowIfDisposed();

            using (var inputStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                outputStream.Position = 0;

                var serializer = new DataContractJsonSerializer(typeof(List<GridReadingDto>));
                return (List<GridReadingDto>)serializer.ReadObject(outputStream);
            }
        }

        // Kompresuje fajl na disku (za arhiviranje)
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
            Console.WriteLine(string.Format("[Compressor] File: {0} -> {1} bytes ({2:F1}% reduction)",
                originalSize, compressedSize, (1.0 - (double)compressedSize / originalSize) * 100));
        }

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

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DataCompressor));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;
        }

        ~DataCompressor()
        {
            Dispose(false);
        }
    }
}