using System;
using System.IO;
using System.Collections.Concurrent;

namespace NENA
{
    public sealed class FileSystemWatcherService
    {
        private readonly FileSystemWatcher _watcher;
        private readonly BlockingCollection<string> _fileQueue;

        public FileSystemWatcherService(BlockingCollection<string> queue)
        {
            _fileQueue = queue;
            _watcher = new FileSystemWatcher
            {
                Path = Config.Instance.UploadsPath,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            _watcher.Created += OnCreated;
//            _watcher.Changed += OnChanged;
            _watcher.Deleted += OnDeleted;
            _watcher.EnableRaisingEvents = true;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File created: {e.FullPath}");
            _fileQueue.Add(e.FullPath);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
        
            Console.WriteLine($"File changed: {e.FullPath}");
            _fileQueue.Add(e.FullPath);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File deleted: {e.FullPath}");
        }
    }
}
