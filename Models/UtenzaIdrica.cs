using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    
    /*
        QUesta classe rappresenta un'utenza idrica associata a un dichiarante.
        Contiene i dati relativi all'utenza idrica, come l'acquedotto, lo stato, il periodo di validità,
        la matricola del contatore, l'indirizzo di ubicazione, i dati anagrafici del titolare e altri dettagli rilevanti.
        Viene utilizzata per memorizzare le utenze idriche che possono beneficiare del bonus idrico.

        CAMPI OBBLIGATORI:
        - idAcquedotto
        - stato
        - periodoIniziale
        - matricolaContatore
        - indirizzoUbicazione
        - numeroCivico
        - tipoUtenza
        - cognome
        - nome
        - codiceFiscale
        - IdEnte
        - IdUser (utente che ha creato o aggiornato il record)

        CAMPI FACOLTATIVI:
        - periodoFinale
        - subUbicazione
        - scalaUbicazione
        - piano
        - interno
        - sesso
        - DataNascita
        - partitaIva
        - IdDichiarante (collegamento al dichiarante, se presente)
        - data_creazione (utile per tracking)
        - data_aggiornamento (utile per tracking)
    
    */
    public class UtenzaIdrica
    {
        // Chiave primaria
        [Key]
        public int id { get; set; }

        [Required]
        public required string? idAcquedotto { get; set; } // Identificativo dell'acquedotto associato all'utenza idrica ricevuto dal sistema Piranha

        [Required]
        public int? stato { get; set; }     // Stato dell'utenza idrica

        [Required]
        public DateTime? periodoIniziale { get; set; }      // Data di inizio del periodo di validità dell'utenza idrica

        public DateTime? periodoFinale { get; set; }     // Data di fine del periodo di validità dell'utenza idrica. Campo facoltativo, può essere null se l'utenza è ancora attiva.

        [Required]
        public required string? matricolaContatore { get; set; }     // Matricola del contatore associato all'utenza idrica

        [Required]
        public required string? indirizzoUbicazione { get; set; }   // Indirizzo di ubicazione dell'utenza idrica

        [Required]
        public required string? numeroCivico { get; set; }  // Numero civico dell'indirizzo di ubicazione dell'utenza idrica

        // Campi facoltativi per ulteriori dettagli sull'ubicazione
        public string? subUbicazione { get; set; }

        public string? scalaUbicazione { get; set; }

        public string? piano { get; set; }

        public string? interno { get; set; }

        // Dati anagrafici del titolare dell'utenza idrica
        // Alcuni campi sono obbligatori per garantire l'identificazione del titolare

        [Required]
        public required string tipoUtenza { get; set; } // Esempio: "Domestica", "Non Domestica"

        [Required]
        public required string? cognome { get; set; }

        [Required]
        public required string? nome { get; set; }

        public string? sesso { get; set; }

        public DateTime? DataNascita { get; set; }

        [Required]
        public required string? codiceFiscale { get; set; }

        public string? partitaIva { get; set; }

        public int? IdDichiarante { get; set; }  // Collegamento al dichiarante, se presente

        // Campi di controllo

        public DateTime? data_creazione { get; set; }

        public DateTime? data_aggiornamento { get; set; }

        [Required]
        public int IdEnte { get; set; }

        public int IdUser { get; set; }

        public int? idToponimo { get; set; }

        // Override del metodo ToString per una rappresentazione leggibile dell'oggetto
        public override string ToString()
        {
            return $"UtenzaIdrica: Acquedotto: {idAcquedotto}, Stato: {stato}, " +
                $"Periodo Iniziale: {(periodoIniziale.HasValue ? periodoIniziale.Value.ToString("dd/MM/yyyy HH:mm:ss") : "")}, " +
                $"Periodo Finale: {(periodoFinale.HasValue ? periodoFinale.Value.ToString("dd/MM/yyyy HH:mm:ss") : "")}, " +
                $"Matricola Contatore: {matricolaContatore}, Indirizzo: {indirizzoUbicazione}, " +
                $"Numero Civico: {numeroCivico}, Tipo Utenza: {tipoUtenza}, Cognome: {cognome}, Nome: {nome}, Codice Fiscale: {codiceFiscale}, Partita IVA {partitaIva}";
        }
    }
}