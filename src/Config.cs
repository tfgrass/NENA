using System;

namespace NENA
{
    public sealed class Config
    {
        private static readonly Lazy<Config> instance = new(() => new Config());

        // Configuration properties
        public string? UploadsPath { get; private set; }
        public string? OutputFormats { get; private set; }
        public string? Cdn77ApiKey { get; private set; }
        public string? Cdn77CacheId { get; private set; }
        public bool Debug { get; private set; }

        // Private constructor to enforce singleton
        private Config()
        {
            LoadConfiguration();

            if(Debug)
            {
                PrintConfiguration();
            }
        }

        // Access the single instance
        public static Config Instance => instance.Value;

        // Load environment variables and CLI args (if needed)
        private void LoadConfiguration()
        {
            UploadsPath = Environment.GetEnvironmentVariable("UPLOADS_PATH") ?? "/app/uploads";
            OutputFormats = Environment.GetEnvironmentVariable("OUTPUT_FORMATS") ?? "avif";
            Cdn77ApiKey = Environment.GetEnvironmentVariable("CDN77_API_KEY") ?? string.Empty;
            Cdn77CacheId = Environment.GetEnvironmentVariable("CDN77_CACHE_ID") ?? string.Empty;
            Debug = Environment.GetEnvironmentVariable("DEBUG")?.ToLower() == "true";

            // Fetch CLI arguments
            string[] args = Environment.GetCommandLineArgs();

            // Process CLI arguments
            foreach (string arg in args)
            {
                if (arg == "--debug")
                {
                    Debug = true;
                }
            }
        }


        // Print config for debugging
        public void PrintConfiguration()
        {
            Console.WriteLine($"UploadsPath: {UploadsPath}");
            Console.WriteLine($"OutputFormats: {OutputFormats}");
            Console.WriteLine($"Cdn77ApiKey: {Cdn77ApiKey}");
            Console.WriteLine($"Cdn77CacheId: {Cdn77CacheId}");
            Console.WriteLine($"Debug: {Debug}");
        }
    }
}
