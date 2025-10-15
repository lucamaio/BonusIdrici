using System;
using System.ComponentModel.DataAnnotations;

namespace Models.ViewModels
{
    public class ReportViewModel
    {
        [Key]
        public int? id { get; set; }

        public required string mese { get; set; }

        public required string anno { get; set; }

        public required string stato { get; set; }

        public required int serie { get; set; }

        [Required]
        public required DateTime DataCreazione { get; set; }

        public DateTime? DataAggiornamento { get; set; }

        public int idEnte { get; set; }
        public string? Username { get; set; }
        
        public int? count { get; set; }

    }
}