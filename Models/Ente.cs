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

        [Required]
        public required bool Piranha { get; set; }
        [Required]
        public required bool Selene { get; set; }

        public required int IdUser { get; set; }
        
        public DateTime? DataCreazione { get; set; }
        
        public DateTime? DataAggiornamento { get; set; }
    }
}
