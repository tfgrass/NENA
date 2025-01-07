using System;
using System.Threading;
using Serilog;
using Serilog.Events;
using System.Collections.Concurrent;

namespace NENA
{
    class Program
    {
        static void Main(string[] args)
        {
            Config.Instance.PrintConfiguration();

            // Determine the minimum log level
            LogEventLevel minLevel = Config.Instance.Debug
                ? LogEventLevel.Verbose
                : LogEventLevel.Debug;

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(minLevel)
                .WriteTo.Console()
                .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                var fileQueue = new BlockingCollection<string>();
                var processor = new QueueProcessor(fileQueue, maxConcurrency: 12);
                var scanner = new FileSystemScanner(fileQueue);

                processor.StartProcessing();

                while (true)
                {
                    // If the scanner is not currently scanning, start a new scan
                    if (!scanner.IsBusy)
                    {
                        Log.Information("Starting new directory scan...");
                        scanner.ScanDirectory();
                    }

                    // Sleep for a bit before checking again
                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Exception was thrown.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
