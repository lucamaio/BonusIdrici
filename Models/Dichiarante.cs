using System.ComponentModel.DataAnnotations;

namespace BonusIdrici2.Models
{
    public class Dichiarante
    {
        [Key]

        public int? id { get; set; }

        [Required]
        public required string Cognome { get; set; }

        [Required]
        public required string Nome { get; set; }

        [Required]
        public required string CodiceFiscale { get; set; }

        [Required]
        public required string Sesso { get; set; }

        [Required]
        public required DateTime DataNascita { get; set; }

        public string? ComuneNascita { get; set; }

        [Required]
        public required string IndirizzoResidenza { get; set; }

        [Required]

        public required string NumeroCivico { get; set; }

        public int? CodiceAbitante {get; set; }
        public int? CodiceFamiglia { get; set; }
        public string? Parentela { get; set; }
        public string? CodiceFiscaleIntestatarioScheda { get; set; }

        public int? NumeroComponenti { get; set; }
       
       [Required]
        public required int IdEnte { get; set; } // ID dell'ente associato al dichiarante

        [Required]
        public required int IdUser { get; set; }

        public DateTime? data_creazione { get; set; }

        public DateTime? data_aggiornamento { get; set; }

        public DateTime? data_cancellazione { get; set; }

        public string? toString()
        {
            return $"Dichiarante: {Cognome}, {Nome}, Codice Fiscale: {CodiceFiscale}, " +
                   $"Sesso: {Sesso}, " +
                   $"Indirizzo Residenza: {IndirizzoResidenza}, Numero Civico: {NumeroCivico}";
        }
    }
}