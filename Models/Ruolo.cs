using System;
using System.ComponentModel.DataAnnotations;
using BonusIdrici2.Data;

namespace BonusIdrici2.Models
{
    public class Ruolo
    {
        [Key]
        public int id { get; set; }

        [required]
        public required string nome { get; set;}

        public DateTime? dataCrezione { get; set; }
        
    }
}
