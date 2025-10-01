// using System;
using System.ComponentModel.DataAnnotations;
using System.Text;

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

        // Dati di tracking
        public DateTime? data_creazione { get; set; }

        public DateTime? data_aggiornamento { get; set; }

        // Ente a cui appartiene il toponimo
        [Required]
        public required int IdEnte { get; set; }

        // Override del metodo ToString per una rappresentazione leggibile dell'oggetto
        public override string ToString()
        {
            return $"Id: {id} Denominazione: {denominazione} Normalizzazione {normalizzazione} Data Creazione: {data_creazione} Data Aggiornamento {data_aggiornamento} Id Ente: {IdEnte}";
        }

    }
}