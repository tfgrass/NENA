using System;
using System.Collections.Concurrent;
using System.IO;
using Serilog;

namespace NENA
{
    public sealed class FileSystemScanner : IDisposable
    {
        private readonly BlockingCollection<string> _fileQueue;
        private readonly string _uploadsPath;
        private readonly FileSystemWatcher _watcher;

        // Tracks whether the scanner is in the middle of scanning
        private bool _isScanning;

        /// <summary>
        /// Indicates if the scanner is currently scanning.
        /// </summary>
        public bool IsBusy => _isScanning;

        public FileSystemScanner(BlockingCollection<string> queue)
        {
            _fileQueue = queue;

            _uploadsPath = Config.Instance.UploadsPath!;

            _watcher = new FileSystemWatcher(_uploadsPath)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName 
                               | NotifyFilters.LastWrite 
                               | NotifyFilters.CreationTime
            };

            _watcher.Created += OnCreated;
            _watcher.Deleted += OnDeleted;
        }
        
        /// <summary>
        /// Scans the specified directory (or the default uploads path if none is provided),
        /// adding files to the queue. This method won't run if a scan is already in progress.
        /// </summary>
        /// <param name="directory">The directory to scan; defaults to _uploadsPath if null.</param>
        public void ScanDirectory(string directory = null)
        {
            // If we are already scanning, skip
            if (_isScanning)
            {
                Log.Verbose("ScanDirectory called while another scan is already in progress. Skipping new scan request.");
                return;
            }

            // Mark as busy
            _isScanning = true;

            // Use a local variable to determine which directory to scan
            var targetDirectory = directory ?? _uploadsPath;

            try
            {
                Log.Verbose($"Scanning directory: {targetDirectory}");

                foreach (var file in Directory.EnumerateFiles(
                             targetDirectory, "*.*", SearchOption.AllDirectories))
                {
                    _fileQueue.Add(file);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to scan directory {targetDirectory}.");
            }
            finally
            {
                // Mark as no longer busy
                _isScanning = false;
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
             Log.Verbose($"[INFO] File created: {e.FullPath}");
            _fileQueue.Add(e.FullPath);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            // maybe handle deletion logic here
            Log.Verbose($"File deleted: {e.FullPath}");

        }

        public void Dispose()
        {
            _watcher.Created -= OnCreated;
            _watcher.Deleted -= OnDeleted;
            _watcher.Dispose();
        }
    }
}
