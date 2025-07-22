using System;
using System.ComponentModel.DataAnnotations;

namespace BonusIdrici2.Models
{
    public class UtenzaIdrica
    {

        [Key]
        public int id { get; set; }

        [Required]
        public required string? idAcquedotto { get; set; } // id_acquedotto nel DB

        [Required]
        public int? stato { get; set; }

        // [Required]
        public DateTime? periodoIniziale { get; set; }

        public DateTime? periodoFinale { get; set; }

        // [Required]
        public required string? matricolaContatore { get; set; }

        [Required]
        public required string? indirizzoUbicazione { get; set; }

        [Required]
        public required string? numeroCivico { get; set; }

        public string? subUbicazione { get; set; }

        public string? scalaUbicazione { get; set; }

        public string? piano { get; set; }

        public string? interno { get; set; }
        
        [Required]
        public required string? tipoUtenza { get; set; } // Esempio: "Domestica", "Non Domestica"

        [Required]
        public required string? cognome { get; set; }

        [Required]
        public required string? nome { get; set; }

        [Required]
        public required string? codiceFiscale { get; set; }

        // [Required]
        public int IdEnte { get; set; } 
    }
}