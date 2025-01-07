using System;
using System.IO;

namespace NENA
{
    class Entrypoint
    {
        static void Main(string[] args)
        {
            var pathToWatch = "/app/uploads"; // Directory in Docker to watch
            Config.Instance.PrintConfiguration();
            if (!Directory.Exists(pathToWatch))
            {
                Console.WriteLine($"Directory does not exist: {pathToWatch}");
                return;
            }

            var watcher = new FileSystemWatcher
            {
                Path = pathToWatch,
                Filter = "*.*", // Watch all files
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
            };

            watcher.Created += OnFileCreated;
            watcher.EnableRaisingEvents = true;

            Console.WriteLine($"Watching {pathToWatch} for changes. Press [enter] to exit.");

            // Keep the program running
            Console.ReadLine();
        }

        private static void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"New file created: {e.FullPath}");
        }
    }
}