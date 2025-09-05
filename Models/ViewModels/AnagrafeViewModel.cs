using System;

namespace BonusIdrici2.Models.ViewModels
{
    public class AnagrafeViewModel
    {
        public int? id { get; set; }
        
        public required string Cognome { get; set; }

        public required string Nome { get; set; }

        public required string CodiceFiscale { get; set; }

        public required string Sesso { get; set; }

        public DateTime? DataNascita { get; set; }
        public string? ComuneNascita { get; set; }

        public string? IndirizzoResidenza { get; set; }

        public string? NumeroCivico { get; set; }

        public int? CodiceFamiglia { get; set; }
        public string? Parentela { get; set; }
        public string? CodiceFiscaleIntestatarioScheda { get; set; }

        public int? NumeroComponenti { get; set; }

        public DateTime? data_creazione { get; set;}
        
        public DateTime? data_aggiornamento { get; set; }

        public required int IdEnte { get; set; }
        
    }
}