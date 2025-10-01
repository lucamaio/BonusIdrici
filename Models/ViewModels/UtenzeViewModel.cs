using System;

namespace Models.ViewModels
{
    public class UtenzeViewModel
    {
        public int? id { get; set; }

        public string? idAcquedotto { get; set; }

        public int? stato { get; set; }

        public DateTime? periodoIniziale { get; set; }

        public DateTime? periodoFinale { get; set; }

        public string? matricolaContatore { get; set; }

        public string? indirizzoUbicazione { get; set; }

        public string? numeroCivico { get; set; }

        public string? subUbicazione { get; set; }

        public string? scalaUbicazione { get; set; }

        public string? piano { get; set; }

        public string? interno { get; set; }

        public string? tipoUtenza { get; set; }

        public string? cognome { get; set; }

        public string? nome { get; set; }

        public string? sesso { get; set; }

        public string? codiceFiscale { get; set; }

        public DateTime? data_creazione { get; set; }

        public DateTime? data_aggiornamento { get; set; }

        public required int IdEnte { get; set; }
        
        public int? IdUser { get; set; }
        
    }
}