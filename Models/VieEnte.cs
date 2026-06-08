// using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Controllers;

namespace Models
{
    /*
      
    */
    public class VieEnte
    {
        // Chiave primaria
        [Key]
        public int? id { get; set; }

        [Required]
        public required string denominazione { get; set; }

        public required string tipoVia { get; set; }    

        // Date di creazione e aggiornamento - tengo traccia delle operazioni di inserimento e modifica dei dati
        
        public DateTime? dataCreazione { get; set; }

        public DateTime? dataAggiornamento { get; set; }


        // Ente a cui appartiene il toponimo
        [Required]
        public required int IdEnte { get; set; }

        // Relazione con il toponimo - ovvero con l'indirizzo normalizzato
        
        [Required]
        public required int IdIndirizzoNormalizzato { get; set; }
        

        // Funzione che restiuisce i tipi di vie validi

        
        private static List<string> tipiVieEnteValidi = new List<string>
        {
            "Via","Vicolo","Largo","Piazza","Viale","Corso","Viadotto","Strada","Piazzetta","Parco","Contrada"
        };

        public static List<string> GetTipiVieEnteValidi()
        {
            return tipiVieEnteValidi;
        }

        private static bool IsTipoViaEnteValido(string tipo)
        {
            return tipiVieEnteValidi.Contains(tipo);
        }

        // Funzione ToString per visualizzare la via in modo leggibile
        public override string ToString()
        {
            return $"{denominazione} ({tipoVia}, IdEnte: {IdEnte}, IdIndirizzoNormalizzato: {IdIndirizzoNormalizzato})";
        }

        [SetsRequiredMembers]
        public VieEnte()
        {
            denominazione = string.Empty;
            tipoVia = string.Empty;
            IdEnte = 0;
            IdIndirizzoNormalizzato = 0;
            this.dataCreazione = DateTime.Now;
            this.dataAggiornamento = null;
        }

        [SetsRequiredMembers]
        public VieEnte(int id, string denominazione, string tipoVia, int idEnte, int idIndirizzoNormalizzato)
        {
            this.id = id;
            this.denominazione = denominazione;
            this.tipoVia = IsTipoViaEnteValido(tipoVia) ? tipoVia : throw new ArgumentException($"Tipo di via non valido: {tipoVia}");
            this.IdEnte = idEnte;
            this.IdIndirizzoNormalizzato = idIndirizzoNormalizzato;
            this.dataCreazione = DateTime.Now;
            this.dataAggiornamento = null;
        }

    }
}
