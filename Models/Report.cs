using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BonusIdrici2.Models
{
    public class Report
    {

        [Key]
        public int id { get; set; }

        public string? idAto { get; set; }

        [Required]
        public required string codiceBonus { get; set; }

        // [Required]
        // public required string IdRichiesta { get; set; }

        public required string esitoStr { get; set; }  // esito "Si" o "No"

        public required string esito { get; set; }  // (1<valore>5) 

        public int? idFornitura { get; set; }

        public string? codiceFiscale { get; set; }

        public int? numeroComponenti { get; set; }
        public string? nomeDichiarante { get; set; }
        public string? cognomeDichiarante { get; set; }

        public string? annoValidita { get; set; }

        public string? indirizzoAbitazione { get; set; }

        public string? numeroCivico { get; set; }

        public string? istat { get; set; }

        public string? capAbitazione { get; set; }

        public string? provinciaAbitazione { get; set; }

        public string? presenzaPod { get; set; }

        [Required]
        public required DateTime? dataInizioValidita { get; set; }

        [Required]
        public required DateTime? dataFineValidita { get; set; }
    
        public DateTime? DataCreazione { get; set; }
        
        [Required]
        public required int IdEnte { get; set; }
    }
}