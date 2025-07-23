using System;

namespace BonusIdrici2.Models
{
    public class Ente
    {
        // [Key]

        public int id { get; set; }

        // [Required]

        public required string nome { get; set; }

        // [Required]

        public required string istat { get; set; }

        // [Required]

        public required string partitaIva { get; set; }

        public string? CodiceFiscale { get; set; }
        
        public string? Cap { get; set; }
        
        public string? Provincia { get; set; }
        
        public string? Regione { get; set; }
    }
}
