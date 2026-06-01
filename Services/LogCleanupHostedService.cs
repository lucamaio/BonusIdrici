using Microsoft.Extensions.Options;

namespace BonusIdrici2.Services
{
    public class LogCleanupHostedService : BackgroundService
    {
        private readonly IWebHostEnvironment environment;
        private readonly ILogger<LogCleanupHostedService> logger;
        private readonly LogCleanupOptions options;

        public LogCleanupHostedService(
            IWebHostEnvironment environment,
            IOptions<LogCleanupOptions> options,
            ILogger<LogCleanupHostedService> logger)
        {
            this.environment = environment;
            this.options = options.Value;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await CheckAndCleanupAsync(stoppingToken);

            using var timer = new PeriodicTimer(GetCheckInterval());

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CheckAndCleanupAsync(stoppingToken);
            }
        }

        private async Task CheckAndCleanupAsync(CancellationToken stoppingToken)
        {
            try
            {
                var stateFilePath = ResolvePath(options.StateFilePath);
                var lastCleanupUtc = await ReadLastCleanupUtcAsync(stateFilePath, stoppingToken);
                var nowUtc = DateTime.UtcNow;

                if (lastCleanupUtc is null)
                {
                    await WriteLastCleanupUtcAsync(stateFilePath, nowUtc, stoppingToken);
                    logger.LogInformation("Pulizia log inizializzata. Prossima pulizia tra {RetentionDays} giorni.", GetRetentionDays());
                    return;
                }

                if (nowUtc - lastCleanupUtc.Value < TimeSpan.FromDays(GetRetentionDays()))
                {
                    return;
                }

                var clearedFiles = ClearLogFiles();
                await WriteLastCleanupUtcAsync(stateFilePath, nowUtc, stoppingToken);

                logger.LogInformation("Pulizia log completata. File svuotati: {ClearedFiles}.", clearedFiles);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore durante la pulizia automatica dei file di log.");
            }
        }

        private int ClearLogFiles()
        {
            var logDirectory = ResolvePath(options.DirectoryPath);

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
                return 0;
            }

            var clearedFiles = 0;

            foreach (var logFile in Directory.EnumerateFiles(logDirectory, "*.log", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    using var stream = new FileStream(logFile, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite);
                    clearedFiles++;
                }
                catch (IOException ex)
                {
                    logger.LogWarning(ex, "Impossibile svuotare il file di log {LogFile}.", logFile);
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.LogWarning(ex, "Permessi insufficienti per svuotare il file di log {LogFile}.", logFile);
                }
            }

            return clearedFiles;
        }

        private async Task<DateTime?> ReadLastCleanupUtcAsync(string stateFilePath, CancellationToken stoppingToken)
        {
            if (!File.Exists(stateFilePath))
            {
                return null;
            }

            var rawValue = await File.ReadAllTextAsync(stateFilePath, stoppingToken);

            return DateTime.TryParse(rawValue, null, System.Globalization.DateTimeStyles.RoundtripKind, out var value)
                ? value.ToUniversalTime()
                : null;
        }

        private static async Task WriteLastCleanupUtcAsync(string stateFilePath, DateTime valueUtc, CancellationToken stoppingToken)
        {
            var directory = Path.GetDirectoryName(stateFilePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(stateFilePath, valueUtc.ToString("O"), stoppingToken);
        }

        private TimeSpan GetCheckInterval()
        {
            return TimeSpan.FromHours(Math.Max(1, options.CheckEveryHours));
        }

        private int GetRetentionDays()
        {
            return Math.Max(1, options.RetentionDays);
        }

        private string ResolvePath(string path)
        {
            return Path.IsPathRooted(path)
                ? path
                : Path.Combine(environment.ContentRootPath, path);
        }
    }
}
