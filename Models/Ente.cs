using System;
using System.ComponentModel.DataAnnotations;

namespace BonusIdrici2.Models
{
    public class Ente
    {
        [Key]
        public int id { get; set; }

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

        [Required]
        public required int Serie { get; set; }

        [Required]
        public required bool Piranha { get; set; }

        [Required]
        public required bool Selene { get; set; }

        [Required]
        public required int IdUser { get; set; }

        [Required]
        public required DateTime DataCreazione { get; set; }

        public DateTime? DataAggiornamento { get; set; }

        public override string ToString()
        {
            return $"id: {id} | nome: {nome} | istat: {istat} | cap: {Cap} | Codice Fiscale {CodiceFiscale} | Data Creazione: {DataCreazione.ToString("dd/MM/yyyy")} | Data Aggiornamento: {DataAggiornamento?.ToString("dd/MM/yyyy")}";
        }
    }
}
