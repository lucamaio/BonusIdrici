using System;
using System.ComponentModel.DataAnnotations;

namespace BonusIdrici2.Models
{
    public class UtenzaIdrica
    {

        [Key]
        public int id { get; set; }

        [Required]
        public required string? idAcquedotto { get; set; } // id_acquedotto nel DB

        [Required]
        public int? stato { get; set; }

        [Required]
        public DateTime? periodoIniziale { get; set; }

        public DateTime? periodoFinale { get; set; }

        [Required]
        public required string? matricolaContatore { get; set; }

        [Required]
        public required string? indirizzoUbicazione { get; set; }

        [Required]
        public required string? numeroCivico { get; set; }

        public string? subUbicazione { get; set; }

        public string? scalaUbicazione { get; set; }

        public string? piano { get; set; }

        public string? interno { get; set; }

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

        public DateTime? data_creazione { get; set; }
        
        public DateTime? data_aggiornamento { get; set; }

        [Required]
        public int IdEnte { get; set; }

        public int IdUser { get; set; }

        public int? idToponimo { get; set; }
        
       public string? ToString()
        {
            return $"UtenzaIdrica: Acquedotto: {idAcquedotto}, Stato: {stato}, " +
                $"Periodo Iniziale: {(periodoIniziale.HasValue ? periodoIniziale.Value.ToString("dd/MM/yyyy HH:mm:ss") : "")}, " +
                $"Periodo Finale: {(periodoFinale.HasValue ? periodoFinale.Value.ToString("dd/MM/yyyy HH:mm:ss") : "")}, " +
                $"Matricola Contatore: {matricolaContatore}, Indirizzo: {indirizzoUbicazione}, " +
                $"Numero Civico: {numeroCivico}, Tipo Utenza: {tipoUtenza}, Cognome: {cognome}, Nome: {nome}, Codice Fiscale: {codiceFiscale}, Partita IVA {partitaIva}";
        }
    }
}