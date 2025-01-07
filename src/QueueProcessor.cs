using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NENA
{
    public sealed class QueueProcessor
    {
        private readonly BlockingCollection<string> _fileQueue;
        private readonly SemaphoreSlim _semaphore;
        private readonly string[] _outputFormats;
        private readonly string _uploadsPath;

        public QueueProcessor(BlockingCollection<string> queue, int maxConcurrency = 8)
        {
            _fileQueue = queue;
            _semaphore = new SemaphoreSlim(maxConcurrency);
            _uploadsPath = Config.Instance.UploadsPath ?? "/opt/app/uploads";
            _outputFormats = Config.Instance.OutputFormats?.Split(',') ?? new[] { "avif" };
        }

        public void StartProcessing()
        {
            Task.Run(() =>
            {
                foreach (var file in _fileQueue.GetConsumingEnumerable())
                {
                    _ = ProcessFileAsync(file);
                }
            });
        }

        private async Task ProcessFileAsync(string filePath)
        {
            if (!NeedsProcessing(filePath)) return;

            await _semaphore.WaitAsync();
            try
            {
                foreach (var format in _outputFormats)
                {
                    string targetPath = ConvertToFormatPath(filePath, format);

                    if (File.Exists(targetPath) && new FileInfo(targetPath).Length > 0)
                    {
                        Console.WriteLine($"[SKIP] {targetPath} already exists.");
                        continue;
                    }

                    string timerLabel = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Converting {Path.GetFileName(filePath)} -> {targetPath}";
                    Console.WriteLine(timerLabel);
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                    // Simulated conversion logic (replace with actual image conversion)
                    await Task.Delay(500); // Simulate file processing
                    File.Copy(filePath, targetPath, true); 

                    stopwatch.Stop();
                    Console.WriteLine($"{timerLabel} - Completed in {stopwatch.ElapsedMilliseconds}ms");

                    PurgeFromCDN(targetPath);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to process {filePath}: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private bool NeedsProcessing(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            if (extension is not (".jpg" or ".jpeg" or ".png"))
                return false;

            foreach (var format in _outputFormats)
            {
                string targetPath = ConvertToFormatPath(filePath, format);
                if (!File.Exists(targetPath) || new FileInfo(targetPath).Length == 0)
                {
                    return true;
                }
            }
            return false;
        }

        private string ConvertToFormatPath(string filePath, string format)
        {
            string relativePath = filePath.Replace(_uploadsPath, "").TrimStart('/');
            return Path.Combine(_uploadsPath, format, relativePath).Replace(Path.GetExtension(filePath), $".{format}");
        }

        private void PurgeFromCDN(string filePath)
        {
            if (string.IsNullOrEmpty(Config.Instance.Cdn77ApiKey) || string.IsNullOrEmpty(Config.Instance.Cdn77CacheId))
                return;

            // Simulate CDN purge request
            Console.WriteLine($"[CDN PURGE] Request to purge {filePath} from cache.");
            // Implement CDN API call if needed
        }
    }
}
