namespace BonusIdrici2.Services
{
    public class LogCleanupOptions
    {
        public const string SectionName = "LogCleanup";

        public string DirectoryPath { get; set; } = "wwwroot/log";

        public int RetentionDays { get; set; } = 90;

        public int CheckEveryHours { get; set; } = 24;

        public string StateFilePath { get; set; } = "App_Data/log-cleanup.state";
    }
}
