using System;

namespace BonusIdrici2.Models.ViewModels
{
    public class EntiViewModel
    {
        public int id { get; set; }
        
        public string nome { get; set; }

        public string istat { get; set; }

        public string? partitaIva { get; set; }

        public string? CodiceFiscale { get; set; }

        public string Cap { get; set; }

        public string? Provincia { get; set; }

        public string? Regione { get; set; }
        
        // public bool? Nostro { get; set; }

        // public DateTime? data_creazione { get; set;}
        
        // public DateTime? data_aggiornamento { get; set; }
        
    }
}