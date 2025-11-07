using System.ComponentModel.DataAnnotations;
using Data;

namespace Models
{

    /*
        Questa classe rappresenta un utente del sistema di gestione dei bonus idrici.
        Contiene le informazioni di autenticazione e i dati anagrafici dell'utente.
        Viene utilizzata per memorizzare i dati degli utenti che possono accedere al sistema.

        CAMPI OBBLIGATORI:
            - Email
            - Password
            - Username
            - idRuolo (1=ADMIN, 2=OPERATORE)

        CAMPI FACOLTATIVI:
            - Cognome
            - Nome
            - dataCreazione (data di creazione del record)
            - dataAggiornamento (data di ultimo aggiornamento del record)

    */
    public class User
    {
        // Chiave primaria
        [Key]
        public int id { get; set; }

        // DATI DI AUTENTICAZIONE OBBLIGATORI

        [Required]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }

        [Required]
        public required string Username { get; set; }

        // DATI ANAGRAFICI FACOLTATIVI
        public string? Cognome { get; set; }

        public string? Nome { get; set; }

        // Ruolo dell'utente nel sistema (1=ADMIN, 2=OPERATORE)
        [Required]
        public required int idRuolo { get; set; }

        // Dati di tracking

        public DateTime? dataCreazione { get; set; }

        public DateTime? dataAggiornamento { get; set; }

        public DateTime? DataAggiornamentoPassword { get; set; }

        // Metodo per ottenere la descrizione del ruolo in base all'idRuolo
        public string? getRuolo()
        {
            switch (idRuolo)
            {
                case 1: return "ADMIN";
                case 2: return "OPERATORE";
                default: return "N/A";

            }
        }
        // Override del metodo ToString per una rappresentazione leggibile dell'oggetto
        public override string ToString()
        {
            return $"Username: {Username} | Cognome: {Cognome} | Nome: {Nome} | Email: {Email} | Ruolo: {getRuolo()}";
        }

    }
}