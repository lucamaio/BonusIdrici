namespace Models.ViewModels
{
    public class SnapshotSummaryViewModel
    {
        public int IdEnte { get; set; }
        public int AnnoRiferimento { get; set; }
        public int MeseRiferimento { get; set; }
        public int NumeroRecord { get; set; }
        public DateTime PrimaImportazione { get; set; }
        public DateTime UltimaImportazione { get; set; }
    }
}
