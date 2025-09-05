// using System;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BonusIdrici2.Models
{
    public class Toponimo
    {
        [Key]
        public int? id { get; set; }

        [Required]
        public required string denominazione { get; set; }

        public string? normalizzazione { get; set; }

        public DateTime? data_creazione { get; set; }

        public DateTime? data_aggiornamento { get; set; }

        [Required]
        public required int IdEnte { get; set; }

        // [Required]
        // public required int IdUser { get; set; }
        // Aggiungere identificativo all'utente che effetua la modifica

        public override string ToString()
        {
            return $"Id: {id} Denominazione: {denominazione} Normalizzazione {normalizzazione} Data Creazione: {data_creazione} Data Aggiornamento {data_aggiornamento} Id Ente: {IdEnte}";
        }

    }
}