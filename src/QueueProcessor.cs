using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NENA
{
    public sealed class QueueProcessor
    {
        private readonly BlockingCollection<string> _fileQueue;

        public QueueProcessor(BlockingCollection<string> queue)
        {
            _fileQueue = queue;
        }

        public void StartProcessing()
        {
            Task.Run(() =>
            {
                foreach (var file in _fileQueue.GetConsumingEnumerable())
                {
                    ProcessFile(file);
                }
            });
        }

        private void ProcessFile(string filePath)
        {
            Console.WriteLine($"Processing file: {filePath}");
            // Add your file processing logic here
        }
    }
}
