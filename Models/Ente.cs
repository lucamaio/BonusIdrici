using System;
using System.ComponentModel.DataAnnotations;

namespace BonusIdrici2.Models
{
    public class Ente
    {
        [Key]

        public int id { get; set; }

        [Required]

        public required string nome { get; set; }

        [Required]

        public required string istat { get; set; }

        [Required]

        public required string partitaIva { get; set; }

        public string? CodiceFiscale { get; set; }

        [Required]

        public required string Cap { get; set; }

        public string? Provincia { get; set; }

        public string? Regione { get; set; }

        public bool? Nostro { get; set; }
        
        public DateTime? data_creazione { get; set;}
        
        public DateTime? data_aggiornamento { get; set; }
    }
}
