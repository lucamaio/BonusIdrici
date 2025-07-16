using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BonusIdrici2.Models
{
    public class FileUpload
    {
        [Key]
        public int Id { get; set; } // Chiave primaria

        [Required]
        [StringLength(255)]
        public string NomeFile { get; set; } // nome_file nel DB

        [Required]
        [StringLength(512)]
        public string PercorsoFile { get; set; } // percorso_file nel DB

        public DateTime? DataInizio { get; set; } // data_inizio_validita nel DB (nullable)
        public DateTime? DataFine { get; set; } // data_fine_validita nel DB (nullable)

        public DateTime? DataCaricamento { get; set; }

        [Required]
        public int IdEnte { get; set; } // id_ente nel DB (chiave esterna)

    }
}