using System.ComponentModel.DataAnnotations;
using BonusIdrici2.Data;

namespace BonusIdrici2.Models
{
    public class User
    {
        [Key]
        public int id { get; set; }

        [Required]

        public required string Email { get; set; }

        public string? Password { get; set; }

        public string? Cognome { get; set; }

        public string? Nome { get; set; }
        
        public string? Username { get; set; }

        [Required]
        public required int idRuolo { get; set; }

        public DateTime? dataCreazione { get; set; }

        public DateTime? dataAggiornamento { get; set; }
        public string? getRuolo()
        {
            switch (idRuolo)
            {
                case 1: return "ADMIN";
                case 2: return "OPERATORE";
                default: return null;

            }
        }

        public string ToString()
        {
            return $"Username: {Username} | Cognome: {Cognome} | Nome: {Nome} | Email: {Email} Ruolo {getRuolo()}";
        }

    }
}