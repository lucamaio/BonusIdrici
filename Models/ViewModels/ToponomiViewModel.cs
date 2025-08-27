using System;

namespace BonusIdrici2.Models.ViewModels
{
    public class ToponomiViewModel
    {
        public int? id { get; set; }
        public required string denominazione { get; set; }
        public string? normalizzazione { get; set; }

        public DateTime? data_creazione { get; set;}
        
        public DateTime? data_aggiornamento { get; set; }

        public int? IdEnte { get; set; }
    }
}