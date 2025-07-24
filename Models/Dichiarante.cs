using System.ComponentModel.DataAnnotations;

namespace BonusIdrici2.Models
{
    public class Dichiarante
    {
        [Key]
        
        public int? IdDichiarante { get; set; }

        [Required]
        public required string Cognome { get; set; }

        [Required]
        public required string Nome { get; set; }

        [Required]
        public required string CodiceFiscale { get; set; }

        [Required]
        public required string Sesso { get; set; }

        [Required]
        public required DateTime? DataNascita { get; set; }
        public string? ComuneNascita { get; set; }

        [Required]
        public required string IndirizzoResidenza { get; set; }

        [Required]

        public required string NumeroCivico { get; set; }

        [Required]
        public required string NomeEnte { get; set; }

        public int? CodiceFamiglia { get; set; }
        public string? Parentela { get; set; }
        public string? CodiceFiscaleIntestatarioScheda { get; set; }

       public int? NumeroComponenti { get; set; }
    }
}