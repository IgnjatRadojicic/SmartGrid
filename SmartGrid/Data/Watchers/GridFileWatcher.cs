using System;
using System.IO;

namespace Data.Watchers
{
    // Prati folder za nove CSV fajlove.
    // Custom delegat + event za obavestavnje.
    public class GridFileWatcher : IDisposable
    {
        private FileSystemWatcher _watcher;
        private bool _disposed = false;

        // Custom delegat
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
                EnableRaisingEvents = false
            };

            _watcher.Created += OnFileCreated;
        }

        public void Start()
        {
            ThrowIfDisposed();
            _watcher.EnableRaisingEvents = true;
            Console.WriteLine("[FileWatcher] Pratim folder: " + _watcher.Path);
        }

        public void Stop()
        {
            ThrowIfDisposed();
            _watcher.EnableRaisingEvents = false;
            Console.WriteLine("[FileWatcher] Zaustavljeno pracenje.");
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("[FileWatcher] Novi fajl: " + e.Name);
            if (FileDetected != null)
                FileDetected(this, new FileDetectedEventArgs(e.FullPath, e.Name));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GridFileWatcher));
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
            Dispose(false);
        }
    }

    public class FileDetectedEventArgs : EventArgs
    {
        public string FullPath { get; private set; }
        public string FileName { get; private set; }
        public DateTime DetectedAt { get; private set; }

        public FileDetectedEventArgs(string fullPath, string fileName)
        {
            FullPath = fullPath;
            FileName = fileName;
            DetectedAt = DateTime.UtcNow;
        }
    }
}