using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class DichiaranteSnapshot
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

        [Required]
        public required string CodiceFiscale { get; set; }

        [Required]
        public required string Cognome { get; set; }

        [Required]
        public required string Nome { get; set; }

        [Required]
        public required string Sesso { get; set; }

        [Required]
        public required DateTime DataNascita { get; set; }

        public string? ComuneNascita { get; set; }

        [Required]
        public required string IndirizzoResidenza { get; set; }

        [Required]
        public required string NumeroCivico { get; set; }

        public string? Parentela { get; set; }

        public int? CodiceFamiglia { get; set; }

        public int? CodiceAbitante { get; set; }

        public int NumeroComponenti { get; set; }

        public string? CodiceFiscaleIntestatarioScheda { get; set; }

        public DateTime? DataCancellazione { get; set; }

        [Required]
        public required DateTime DataImportazione { get; set; }

        [Required]
        public required string HashRecord { get; set; }
    }
}
