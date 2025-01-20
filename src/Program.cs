using System;
using Serilog;

namespace NENA
{
    class Program
    {
        static void Main(string[] args)
        {
            Config.Instance.PrintConfiguration();
            
            // Set up Serilog
            var minLevel = Config.Instance.Debug
                ? Serilog.Events.LogEventLevel.Verbose
                : Serilog.Events.LogEventLevel.Debug;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(minLevel)
                .WriteTo.Console()
                .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                // Create the unique file queue
                var uniqueQueue = new UniqueFileQueue();

                // Pass the same queue to both the scanner & processor
                var processor = new QueueProcessor(uniqueQueue, maxConcurrency: 12);
                var scanner = new FileSystemScanner(uniqueQueue);

                processor.StartProcessing();

                while (true)
                {
                    if (!scanner.IsBusy)
                    {
                        Log.Information("Starting a new directory scan...");
                        scanner.ScanDirectory();
                    }

                    // Sleep a bit to avoid tight loop
                    System.Threading.Thread.Sleep(200000);
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
