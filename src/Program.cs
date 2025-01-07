using System;
using System.Collections.Concurrent;

namespace NENA
{
    class Program
    {
        static void Main(string[] args)
        {
            Config.Instance.PrintConfiguration();

            var fileQueue = new BlockingCollection<string>();
            var processor = new QueueProcessor(fileQueue, maxConcurrency: 12);
            var scanner = new FileSystemScanner(fileQueue);
            
            processor.StartProcessing();
            scanner.ScanDirectory();

            Console.WriteLine("File watcher and scanner running. Press [ENTER] to exit...");
            Console.ReadLine();
        }
    }
}
