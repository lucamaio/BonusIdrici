using System;

namespace BonusIdrici2.Models.ViewModels
{
    public class EntiViewModel
    {
        public int id { get; set; }
        
        public required string nome { get; set; }

        public required string istat { get; set; }

        public required string partitaIva { get; set; }

        public string? CodiceFiscale { get; set; }

        public required string Cap { get; set; }

        public string? Provincia { get; set; }

        public string? Regione { get; set; }
        
        public required int Serie { get; set; }
        public required bool Selene { get; set; }
        
        public required bool Piranha { get; set; }

        public required DateTime DataCreazione { get; set; }
        
        public DateTime? DataAggiornamento { get; set; }
        
    }
}