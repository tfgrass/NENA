using System;
using System.Collections.Concurrent;

namespace NENA
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileQueue = new BlockingCollection<string>();
            var fileWatcher = new FileSystemWatcherService(fileQueue);
            var processor = new QueueProcessor(fileQueue);
            
            processor.StartProcessing();

            Console.WriteLine("File watcher is running. Press [ENTER] to exit...");
            Console.ReadLine();
        }
    }
}
