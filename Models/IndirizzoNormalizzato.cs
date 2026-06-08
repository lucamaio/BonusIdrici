// using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Controllers;

namespace Models
{
    /*
       

    */
    public class IndirizzoNormalizzato
    {
        // Chiave primaria
        [Key]
        public int? id { get; set; }

        [Required]
        public required string denominazione { get; set; }

        public required string stato { get; set; }    

        // Date

        public DateTime? dataCreazione { get; set; }

        public DateTime? dataAggiornamento { get; set; }

        // Valori possibili per lo stato del toponimo

        private static List<string> statiToponimoValidi = new List<string>
        {
            "Da verificare","Verificato","Sospeso","Rifiutato"
        };

        public static List<string> GetStatiToponimoValidi()
        {
            return statiToponimoValidi;
        }

        private static bool IsStatoToponimoValido(string stato)
        {
            return statiToponimoValidi.Contains(stato);
        }

        [SetsRequiredMembers]
        public IndirizzoNormalizzato()
        {
            denominazione = string.Empty;
            stato = string.Empty;
        }

        [SetsRequiredMembers]
        public IndirizzoNormalizzato(string denominazione, string stato)
        {
            this.denominazione = denominazione;
            this.stato = IsStatoToponimoValido(stato) ? stato : throw new ArgumentException($"Stato del toponimo non valido: {stato}");
            this.dataCreazione = DateTime.Now;
            this.dataAggiornamento = null;
        }

        // Costruttore con validazione dello stato del toponimo
        [SetsRequiredMembers]
        public IndirizzoNormalizzato(int id, string denominazione, string stato)
        {
            this.id = id;
            this.denominazione = denominazione;
            this.stato = IsStatoToponimoValido(stato) ? stato : throw new ArgumentException($"Stato del toponimo non valido: {stato}");
            this.dataCreazione = DateTime.Now;
            this.dataAggiornamento = null;
        }
    }
}
