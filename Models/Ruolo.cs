using System;
using System.ComponentModel.DataAnnotations;
using Data;

namespace Models
{
    public class Ruolo
    {
        [Key]
        public int id { get; set; }

        [Required]
        public required string nome { get; set;}

        public DateTime? dataCreazione { get; set; }
        
    }
}
