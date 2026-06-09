namespace Models.ViewModels
{
    public class SectionActivitySummaryViewModel
    {
        public required string SectionName { get; set; }

        public string SectionKey { get; set; } = string.Empty;

        public DateTime? LastUpdate { get; set; }

        public int TotalCount { get; set; }

        public IReadOnlyList<SectionActivityItemViewModel> RecentActivities { get; set; } = new List<SectionActivityItemViewModel>();

        public IReadOnlyList<DiagnosticActivityCategoryViewModel> DiagnosticActivities { get; set; } = new List<DiagnosticActivityCategoryViewModel>();
    }

    public class SectionActivityItemViewModel
    {
        public DateTime? Timestamp { get; set; }

        public required string Operation { get; set; }

        public required string Title { get; set; }

        public string? Detail { get; set; }

        public required string Icon { get; set; }

        public required string Tone { get; set; }

        public required string Controller { get; set; }

        public required string Action { get; set; }

        public int? RouteId { get; set; }

        public string RouteParameterName { get; set; } = "id";
    }

    public class AdminActivityDashboardViewModel
    {
        public IReadOnlyList<SectionActivitySummaryViewModel> Sections { get; set; } = new List<SectionActivitySummaryViewModel>();

        public IReadOnlyList<DiagnosticActivityCategoryViewModel> DiagnosticActivities { get; set; } = new List<DiagnosticActivityCategoryViewModel>();

        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }

    public class AdminActivityDetailViewModel
    {
        public required string SectionKey { get; set; }

        public required string SectionName { get; set; }

        public DateTime? LastUpdate { get; set; }

        public IReadOnlyList<SectionActivityItemViewModel> Activities { get; set; } = new List<SectionActivityItemViewModel>();
    }

    public class DiagnosticActivityCategoryViewModel
    {
        public required string Level { get; set; }

        public required string Title { get; set; }

        public required string Icon { get; set; }

        public required string Tone { get; set; }

        public int TotalCount { get; set; }

        public IReadOnlyList<DiagnosticActivityItemViewModel> RecentEvents { get; set; } = new List<DiagnosticActivityItemViewModel>();
    }

    public class DiagnosticActivityItemViewModel
    {
        public DateTime? Timestamp { get; set; }

        public required string Message { get; set; }

        public required string SourceKey { get; set; }

        public required string SourceTitle { get; set; }
    }
}
