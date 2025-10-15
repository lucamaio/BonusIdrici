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

        // Costruttori

        // public Report(){

        // }

        // Funzione che mi restituisce l'elenco degli stati validi

        public List<string> getStatiValidi()
        {
            List<string> stati = ["Da Verificare", "Approvato", "Emesso"];
            return stati;
        }

        // Metodo ToString()

        public override string ToString(){
            return $"Report: Id: {id}, Mese elaborazione {mese}, Anno elaborazione: {anno}, stato {stato}, "+
            $"Data Creazione: {DataCreazione}, Data Aggiornamento: {DataAggiornamento}, Id Ente: {idEnte}, Id Utente {idUser}";
        }   
        

    }
}