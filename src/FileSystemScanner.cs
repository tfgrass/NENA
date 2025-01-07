using System;
using System.Collections.Concurrent;
using System.IO;

namespace NENA
{
    public sealed class FileSystemScanner : IDisposable
    {
        private readonly BlockingCollection<string> _fileQueue;
        private readonly string _uploadsPath;
        private readonly FileSystemWatcher _watcher;

        public FileSystemScanner(BlockingCollection<string> queue)
        {
            _fileQueue = queue;
            _uploadsPath = Config.Instance.UploadsPath ?? "/opt/app/uploads";

            _watcher = new FileSystemWatcher(_uploadsPath)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
            };

            _watcher.Created += OnCreated;
            _watcher.Deleted += OnDeleted;
        }

        public void ScanDirectory(string directory = null)
        {
            Console.WriteLine($"Scanning directory: {directory ?? _uploadsPath}");
            directory ??= _uploadsPath;

            try
            {
                foreach (var file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
                {
                    _fileQueue.Add(file);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to scan directory {directory}: {ex.Message}");
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"[INFO] File created: {e.FullPath}");
            _fileQueue.Add(e.FullPath);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"[INFO] File deleted: {e.FullPath}");
            _fileQueue.Add($"DELETED:{e.FullPath}");
        }

        public void Dispose()
        {
            _watcher.Created -= OnCreated;
            _watcher.Deleted -= OnDeleted;
            _watcher.Dispose();
        }
    }
}
