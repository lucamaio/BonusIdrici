using System;
using System.ComponentModel.DataAnnotations;

namespace BonusIdrici2.Models
{
    /*
        Questa classe rappresenta un ente che gestisce i bonus idrici.
        Contiene i dati identificativi e fiscali dell'ente.
        Viene utilizzata per memorizzare i dati degli enti che partecipano al programma di bonus idrici.

        CAMPI OBBLIGATORI:
        - nome
        - istat
        - partitaIva
        - Cap
        - Serie
        - Piranha (indica se l'ente utilizza il sistema Piranha)
        - Selene (indica se l'ente utilizza il sistema Selene)
        - IdUser (utente che ha creato o aggiornato il record)
        - DataCreazione (data di creazione del record)

        CAMPI FACOLTATIVI:
        - CodiceFiscale
        - Provincia
        - Regione
        - DataAggiornamento (data di ultimo aggiornamento del record)

    */
    public class Ente
    {
        // Chiave primaria
        [Key]
        public int id { get; set; }
        // DATI IDENTIFICATIVI E FISCALI OBBLIGATORI
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

        // DATI RELATIVI AL SISTEMA DI GESTIONE DEGLI ENTI

        [Required]
        public required int Serie { get; set; }

        // Indica se l'ente utilizza il sistema Piranha
        [Required]
        public required bool Piranha { get; set; }

        // Indica se l'ente utilizza il sistema Selene

        [Required]
        public required bool Selene { get; set; }

        // CAMPI DI CONTROLLO

        [Required]
        public required int IdUser { get; set; }

        [Required]
        public required DateTime DataCreazione { get; set; }

        public DateTime? DataAggiornamento { get; set; }

        // Override del metodo ToString per una rappresentazione leggibile dell'oggetto
        public override string ToString()
        {
            return $"id: {id} | nome: {nome} | istat: {istat} | cap: {Cap} | Codice Fiscale {CodiceFiscale} | Data Creazione: {DataCreazione.ToString("dd/MM/yyyy")} | Data Aggiornamento: {DataAggiornamento?.ToString("dd/MM/yyyy")}";
        }
    }
}
