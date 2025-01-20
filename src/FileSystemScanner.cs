using System;
using System.IO;
using Serilog;

namespace NENA
{
    public sealed class FileSystemScanner : IDisposable
    {
        private readonly UniqueFileQueue _fileQueue;   // <- changed
        private readonly string _uploadsPath;
        private readonly string _outputPath;
        private readonly FileSystemWatcher _watcher;

        // Tracks whether the scanner is in the middle of scanning
        private bool _isScanning;

        /// <summary>
        /// Indicates if the scanner is currently scanning.
        /// </summary>
        public bool IsBusy => _isScanning;

        public FileSystemScanner(UniqueFileQueue queue)
        {
            _fileQueue = queue;
            _uploadsPath = Config.Instance.UploadsPath!;
            _outputPath = Path.Combine(_uploadsPath, Config.Instance.OutputFormats!);

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
        /// attempting to enqueue each file. This method won't run if a scan is already in progress.
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

            _isScanning = true;
            var targetDirectory = directory ?? _uploadsPath;

            try
            {
                Log.Verbose($"Scanning directory: {targetDirectory}");

                foreach (var file in Directory.EnumerateFiles(targetDirectory, "*.*", SearchOption.AllDirectories))
                {
                    // Determine the path of the file relative to _uploadsPath
                    var relativePath = Path.GetRelativePath(_uploadsPath, file);

                    // Skip files in the output folder
                    if (file.StartsWith(_outputPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Verbose($"Skipping file in output folder: {file}");
                        continue;
                    }

                    // Otherwise, enqueue the file
                    bool added = _fileQueue.TryAdd(file);
                    if (added)
                        Log.Verbose($"Enqueued: {file}");
                    else
                        Log.Verbose($"Skipped (already in queue): {file}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to scan directory {targetDirectory}.");
            }
            finally
            {
                _isScanning = false;
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            // Skip files in the output folder
            if (e.FullPath.StartsWith(_outputPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                Log.Verbose($"[INFO] Ignored created file in output folder: {e.FullPath}");
                return;
            }

            // Try to enqueue; if it was already in the queue, this is a no-op
            bool added = _fileQueue.TryAdd(e.FullPath);
            Log.Verbose(added
                ? $"[INFO] File created and enqueued: {e.FullPath}"
                : $"[INFO] File created, but already in queue: {e.FullPath}");
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            // If you want special logic for deletions, you could do it here
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
