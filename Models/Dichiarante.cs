using System.ComponentModel.DataAnnotations;

namespace Models
{
    /*
        Questa classe rappresenta l'anagrafe di un persona che fa parte di un ente.
        Contiene i dati anagrafici e di residenza.
        Viene utilizzata per memorizzare i dati del dichiarante e dei componenti del nucleo familiare.
        
        CAMPI OBBLIGATORI:
        - Cognome
        - Nome
        - CodiceFiscale
        - Sesso
        - DataNascita
        - IndirizzoResidenza
        - NumeroCivico
        - IdEnte
        - IdUser (utente che ha creato o aggiornato il record)

        CAMPI FACOLTATIVI:
        - ComuneNascita
        - CodiceAbitante
        - CodiceFamiglia
        - Parentela
        - CodiceFiscaleIntestatarioScheda
        - NumeroComponenti (utile per il dichiarante)
        - data_creazione (utile per tracking)
        - data_aggiornamento (utile per tracking)
        - data_cancellazione (per soft delete)

    */
    public class Dichiarante
    {
        // Chiave primaria
        [Key]
        public int? id { get; set; }

        // DATI ANAGRAFICI OBBLIGATORI

        [Required]
        public required string Cognome { get; set; }

        [Required]
        public required string Nome { get; set; }

        [Required]
        public required string CodiceFiscale { get; set; }

        [Required]
        public required string Sesso { get; set; }

        [Required]
        public required DateTime DataNascita { get; set; }

        public string? ComuneNascita { get; set; }

        [Required]
        public required string IndirizzoResidenza { get; set; }

        [Required]
        public required string NumeroCivico { get; set; }

        // CAMPI FACOLTATIVI RELATIVI AL NUCLEO FAMILIARE
        public int? CodiceAbitante { get; set; }
        public int? CodiceFamiglia { get; set; }
        public string? Parentela { get; set; }
        public string? CodiceFiscaleIntestatarioScheda { get; set; }

        public int NumeroComponenti { get; set; }

        // CAMPI RELATIVI ALL'ENTE DI APPARTENENZA
        [Required]
        public required int IdEnte { get; set; } // ID dell'ente associato al dichiarante

        // CAMPI DI CONTROLLO
        [Required]
        public required int IdUser { get; set; }

        public DateTime? data_creazione { get; set; }

        public DateTime? data_aggiornamento { get; set; }

        public DateTime? data_cancellazione { get; set; }

        // Override del metodo ToString per una rappresentazione leggibile dell'oggetto
        
        public override string ToString()
        {
            return $"Dichiarante: {Cognome}, {Nome}, Codice Fiscale: {CodiceFiscale}, " +
                   $"Sesso: {Sesso}, " + $"Data Nascita: {DataNascita.ToString("yyyy/MM/dd")}, " +
                   $"Indirizzo Residenza: {IndirizzoResidenza}, Numero Civico: {NumeroCivico}";
        }
    }
}