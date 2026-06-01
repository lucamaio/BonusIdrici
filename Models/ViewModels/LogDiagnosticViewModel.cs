namespace Models.ViewModels
{
    public class LogDiagnosticViewModel
    {
        public required List<LogFileDiagnosticViewModel> Files { get; set; }

        public int TotalFiles { get; set; }

        public long TotalBytes { get; set; }

        public int TotalRows { get; set; }

        public int TotalWarnings { get; set; }

        public int TotalErrors { get; set; }

        public DateTime? LastEvent { get; set; }
    }

    public class LogFileDiagnosticViewModel
    {
        public required string Key { get; set; }

        public required string Title { get; set; }

        public required string Description { get; set; }

        public required string Icon { get; set; }

        public required string AccentClass { get; set; }

        public required string Path { get; set; }

        public bool Exists { get; set; }

        public long SizeBytes { get; set; }

        public int Rows { get; set; }

        public int InfoCount { get; set; }

        public int WarningCount { get; set; }

        public int ErrorCount { get; set; }

        public DateTime? LastEvent { get; set; }

        public required string Status { get; set; }

        public required string StatusLabel { get; set; }

        public string SizeLabel => FormatBytes(SizeBytes);

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
            {
                return $"{bytes} B";
            }

            var kb = bytes / 1024d;
            if (kb < 1024)
            {
                return $"{kb:0.##} KB";
            }

            return $"{kb / 1024d:0.##} MB";
        }
    }
}
