// using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Controllers;

namespace Models
{
    /*
        Questa classe rappresenta un toponimo associato a un ente.
        Contiene la denominazione del toponimo e la sua normalizzazione.
        Viene utilizzata per memorizzare i toponimi che possono essere associati ai dichiarante.

        CAMPI OBBLIGATORI:
        - denominazione
        - IdEnte (ente a cui appartiene il toponimo)

        CAMPI FACOLTATIVI:
        - normalizzazione
        - data_creazione (data di creazione del record)
        - data_aggiornamento (data di ultimo aggiornamento del record)

    */
    public class Toponimo
    {
        // Chiave primaria
        [Key]
        public int? id { get; set; }

        [Required]
        public required string denominazione { get; set; }

        public string? normalizzazione { get; set; }        // Campo facoltativo per la normalizzazione del toponimo. Contiene una versione standardizzata del toponimo.

        public string? tipoToponimo { get; set; }    // Campo facoltativo per il tipo di toponimo (es. via, piazza, ecc.)

        public string? intestazione { get; set; }    // Campo facoltativo per l'intestazione del toponimo base

        public string? intestazioneNormalizzata { get; set; }    // Campo facoltativo per l'intestazione normalizzata del toponimo base
        // Dati di tracking
        public DateTime? dataCreazione { get; set; }

        public DateTime? dataAggiornamento { get; set; }

        private static List<string> tipiToponimiValidi = new List<string>
        {
            "Via","Vicolo","Largo","Piazza","Viale","Corso","Viadotto","Strada","Piazzetta","Parco","Contrada"
        };

        // Ente a cui appartiene il toponimo
        [Required]
        public required int IdEnte { get; set; }

        // Metodo che mi ritorna la lista dei tipi di toponimi validi
        public static List<string> GetTipiToponimiValidi()
        {
            return tipiToponimiValidi;
        }

        // Metodo che verifica se il tipo di toponimo è valido
        public static bool IsTipoToponimoValido(string tipo)
        {
           
            // 1. Controllo se il tipo è nella lista dei tipi validi
            if (string.IsNullOrWhiteSpace(tipo) || tipo.Length < 3)
                return false;

            // 2. formatto il tipo
            string tipoFormattato = FunzioniTrasversali.rimuoviVirgolette(tipo).ToLower();
            tipoFormattato = char.ToUpper(tipoFormattato[0]) + tipoFormattato.Substring(1);
            //AccountController.logFile.LogDebug($"Verifica tipo toponimo: '{tipoFormattato}'");
            // 3. Controllo se il tipo formattato è nella lista dei tipi validi
            return tipiToponimiValidi.Contains(tipoFormattato);
        }

        // Override del metodo ToString per una rappresentazione leggibile dell'oggetto
        public override string ToString()
        {
            return $"Id: {id} Denominazione: {denominazione} | Normalizzazione {normalizzazione} | Tipo Toponimo {tipoToponimo ?? "N/D"} | Intestazione: {intestazione ?? "N/D"} | Intestazione Normalizzata: {intestazioneNormalizzata ?? "N/D"} | Data Creazione: {dataCreazione?.ToString("dd/MM/yyyy")} | Data Aggiornamento {dataAggiornamento?.ToString("dd/MM/yyyy")} | Id Ente: {IdEnte}";
        }

    }
}