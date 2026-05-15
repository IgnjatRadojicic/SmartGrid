using System;
using System.IO;
using System.Text;

namespace SmartGrid.Data.Streams
{

    public class StreamTransfer : IDisposable
    {
        private bool _disposed = false;

        /// <summary>
        /// Zapisuje string podatke u MemoryStream i vraca byte array.
        /// Korisno za pripremu podataka pre slanja preko WCF-a.
        /// </summary>
        public byte[] WriteToMemory(string data)
        {
            ThrowIfDisposed();

            using (var memoryStream = new MemoryStream())
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(data);
                writer.Flush();
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Cita byte array iz MemoryStream-a nazad u string
        /// </summary>
        public string ReadFromMemory(byte[] data)
        {
            ThrowIfDisposed();

            using (var memoryStream = new MemoryStream(data))
            using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Kopira sadrzaj jednog fajla u drugi koristeci BufferedStream.
        /// Demonstrira efikasan I/O sa bufferovanjem.
        /// </summary>
        public void BufferedCopy(string sourcePath, string destPath, int bufferSize = 8192)
        {
            ThrowIfDisposed();

            using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
            using (var bufferedSource = new BufferedStream(sourceStream, bufferSize))
            using (var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write))
            using (var bufferedDest = new BufferedStream(destStream, bufferSize))
            {
                byte[] buffer = new byte[bufferSize];
                int bytesRead;

                while ((bytesRead = bufferedSource.Read(buffer, 0, buffer.Length)) > 0)
                {
                    bufferedDest.Write(buffer, 0, bytesRead);
                }
            }

            Console.WriteLine($"[StreamTransfer] Kopirano: {sourcePath} → {destPath}");
        }

        /// <summary>
        /// Arhivira fajl premesta iz Input u Archive folder.
        /// </summary>
        public void ArchiveFile(string sourcePath, string archiveDir)
        {
            ThrowIfDisposed();

            if (!Directory.Exists(archiveDir))
                Directory.CreateDirectory(archiveDir);

            string fileName = Path.GetFileNameWithoutExtension(sourcePath);
            string extension = Path.GetExtension(sourcePath);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string archivePath = Path.Combine(archiveDir, $"{fileName}_{timestamp}{extension}");

            BufferedCopy(sourcePath, archivePath);
            File.Delete(sourcePath);

            Console.WriteLine($"[StreamTransfer] Arhivirano: {sourcePath} → {archivePath}");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StreamTransfer));
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

        ~StreamTransfer()
        {
            Dispose(disposing: false);
        }
    }
}