using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;

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

                    // Actual AVIF (or other format) conversion using Magick.NET
                    try
                    {
                        using var image = new MagickImage(filePath);

                        // You can set various encoder settings here
                        // For AVIF, typical properties might be something like:
                        // - image.Quality = 50; // Just an example – adjusts compression level
                        // - image.Settings.SetDefine(MagickFormat.Avif, "speed", "6"); // Speed vs. quality trade-off (1=best, 10=fastest)
                        // - image.Settings.SetDefine(MagickFormat.Avif, "effort", "6");

                        // Convert the image to the desired format
                        switch (format.ToLower())
                        {
                            case "avif":
                                image.Format = MagickFormat.Avif;
                                break;
                            case "webp":
                                image.Format = MagickFormat.WebP;
                                break;
                            // Add any other formats needed
                            default:
                                // Fallback or handle other formats
                                image.Format = MagickFormat.Png;
                                break;
                        }

                        // Finally, write the converted image
                        image.Write(targetPath);
                    }
                    catch (Exception conversionEx)
                    {
                        Console.Error.WriteLine($"[ERROR] Conversion to {format} failed for {filePath}: {conversionEx.Message}");
                        // Optionally rethrow or continue
                    }

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
