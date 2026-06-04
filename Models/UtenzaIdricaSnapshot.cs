using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class UtenzaIdricaSnapshot
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required int IdEnte { get; set; }

        [Required]
        public required int IdUser { get; set; }

        [Required]
        public required int AnnoRiferimento { get; set; }

        [Required]
        public required int MeseRiferimento { get; set; }

        public int? IdUtenzaOriginale { get; set; }

        public string? IdAcquedotto { get; set; }

        public string? MatricolaContatore { get; set; }

        public int? Stato { get; set; }

        public DateTime? PeriodoIniziale { get; set; }

        public DateTime? PeriodoFinale { get; set; }

        public string? IndirizzoUbicazione { get; set; }

        public string? NumeroCivico { get; set; }

        public string? SubUbicazione { get; set; }

        public string? ScalaUbicazione { get; set; }

        public string? Piano { get; set; }

        public string? Interno { get; set; }

        public string? TipoUtenza { get; set; }

        public string? Cognome { get; set; }

        public string? Nome { get; set; }

        public string? Sesso { get; set; }

        public DateTime? DataNascita { get; set; }

        public string? CodiceFiscale { get; set; }

        public string? PartitaIva { get; set; }

        public int? IdToponimo { get; set; }

        public int? IdDichiarante { get; set; }

        [Required]
        public required DateTime DataImportazione { get; set; }

        [Required]
        public required string HashRecord { get; set; }
    }
}
