using System;
using System.IO;

namespace SmartGrid.Data.Watchers
{
    /// <summary>
    /// Prati Data/Input/ folder za nove CSV fajlove
    /// </summary>
    public class GridFileWatcher : IDisposable
    {
        private FileSystemWatcher _watcher;
        private bool _disposed = false;

        // Delegat za novi fajl
        public delegate void NewFileDetectedHandler(object sender, FileDetectedEventArgs e);

        // Event
        public event NewFileDetectedHandler FileDetected;

        public GridFileWatcher(string watchPath)
        {
            if (!Directory.Exists(watchPath))
                Directory.CreateDirectory(watchPath);

            _watcher = new FileSystemWatcher(watchPath)
            {
                Filter = "*.csv",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = false  // startujemo eksplicitno
            };

            _watcher.Created += OnFileCreated;
        }


        public void Start()
        {
            ThrowIfDisposed();
            _watcher.EnableRaisingEvents = true;
            Console.WriteLine($"[FileWatcher] Pratim folder: {_watcher.Path}");
        }

        public void Stop()
        {
            ThrowIfDisposed();
            _watcher.EnableRaisingEvents = false;
            Console.WriteLine("[FileWatcher] Zaustavljeno praćenje.");
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"[FileWatcher] Detektovan novi fajl: {e.Name}");
            FileDetected?.Invoke(this, new FileDetectedEventArgs(e.FullPath, e.Name));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GridFileWatcher));
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
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Created -= OnFileCreated;
                    _watcher.Dispose();
                    _watcher = null;
                }
            }

            _disposed = true;
        }

        ~GridFileWatcher()
        {
            Dispose(disposing: false);
        }
    }

    public class FileDetectedEventArgs : EventArgs
    {
        public string FullPath { get; }
        public string FileName { get; }
        public DateTime DetectedAt { get; }

        public FileDetectedEventArgs(string fullPath, string fileName)
        {
            FullPath = fullPath;
            FileName = fileName;
            DetectedAt = DateTime.UtcNow;
        }
    }
}