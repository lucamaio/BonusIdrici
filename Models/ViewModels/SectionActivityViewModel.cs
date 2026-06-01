namespace Models.ViewModels
{
    public class SectionActivitySummaryViewModel
    {
        public required string SectionName { get; set; }

        public DateTime? LastUpdate { get; set; }

        public IReadOnlyList<SectionActivityItemViewModel> RecentActivities { get; set; } = new List<SectionActivityItemViewModel>();
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

        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }
}
