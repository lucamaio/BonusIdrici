using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BonusIdrici2.Models
{
    public class Report
    {

        [Key]
        public int id { get; set; }

        [Required]
        public required string idAto { get; set; }

        [Required]
        public required string codiceBonus { get; set; }

        [Required]
        public required string esitoStr { get; set; }  // esito "Si" o "No"

        [Required]
        public required string esito { get; set; }  // (1<valore>5) 

        public int? idFornitura { get; set; }

        [Required]
        public required string codiceFiscaleRichiedente { get; set; }

        public string? codiceFiscaleUtenzaTrovata { get; set; }

        public int? idUtenza { get; set; }

        public int? numeroComponenti { get; set; }

        [Required]
        public required string nomeDichiarante { get; set; }

        [Required]
        public required string cognomeDichiarante { get; set; }

        public int? idDichiarante { get; set; }
        
        [Required]
        public required string annoValidita { get; set; }

        [Required]
        public required string indirizzoAbitazione { get; set; }

        public string? numeroCivico { get; set; }

        [Required]
        public required string istat { get; set; }

        [Required]
        public required string capAbitazione { get; set; }

        public string? provinciaAbitazione { get; set; }

        [Required]
        public required string presenzaPod { get; set; }

        public string? note { get; set; }

        public bool? incongruenze { get; set; }

        [Required]
        public required int serie { get; set; }

        public int? mc { get; set; }

        [Required]
        public required DateTime dataInizioValidita { get; set; }

        [Required]
        public required DateTime dataFineValidita { get; set; }

        [Required]
        public required DateTime DataCreazione { get; set; }

        public DateTime? DataAggiornamento { get; set; }

        [Required]
        public required int IdEnte { get; set; }

        [Required]
        public required int IdUser { get; set; }

        public override string ToString()
        {
            return $"Report: id={id}, codiceBonus={codiceBonus}, esitoStr={esitoStr}, esito={esito}, idFornitura={idFornitura}, codiceFiscale={codiceFiscaleRichiedente}, "+
                $"numeroComponenti ={numeroComponenti}, nomeDichiarante={nomeDichiarante}, cognomeDichiarante={cognomeDichiarante}, annoValidita={annoValidita}, "+
                $"indirizzoAbitazione ={indirizzoAbitazione}, numeroCivico={numeroCivico}, istat={istat}, capAbitazione={capAbitazione}, provinciaAbitazione={provinciaAbitazione}, "+
                $"presenzaPod ={presenzaPod}, dataInizioValidita={dataInizioValidita}, dataFineValidita={dataFineValidita}, DataCreazione={DataCreazione}, IdEnte={IdEnte}";
        }
    }
}