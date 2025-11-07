using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Report
    {
        // Identificatore Univoco
        [Key]
        public int id { get; set; }

        // intestazione
        [Required]
        public required string mese { get; set; }
        [Required]
        public required string anno { get; set; }

        // Stato del report

        public string? stato { get; set; }

        // Metadati 

        public int idUser { get; set; }

        [Required]
        public required DateTime DataCreazione { get; set; }

        public DateTime? DataAggiornamento { get; set; }

        [Required]
        public required int idEnte { get; set; }

        [Required]
        public required int serie {get; set;}

        // Stati validi per il report
        private static List<string> statiValidi = new List<string> { "Da verificare", "Approvato", "Emesso", "Annullato" };

        public List<string> getStatiValidi()
        {
            return statiValidi;
        }

        public bool isStatoValido(string statoDaVerificare)
        {
            return statiValidi.Contains(statoDaVerificare);
        }

        // Metodo ToString()

        public override string ToString(){
            return $"Report: Id: {id}, Mese elaborazione {mese}, Anno elaborazione: {anno}, stato {stato}, "+
            $"Data Creazione: {DataCreazione}, Data Aggiornamento: {DataAggiornamento}, Id Ente: {idEnte}, Id Utente {idUser}";
        }   
        

    }
}