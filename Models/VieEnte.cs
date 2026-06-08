using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Models
{
    public class VieEnte
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IdEnte { get; set; }

        [Required]
        public required string DenominazioneOriginale { get; set; }

        [Required]
        public required string DenominazionePulita { get; set; }

        public string? DenominazioneNormalizzataProposta { get; set; }

        [Required]
        public required string TipologiaVia { get; set; }

        public string? CivicoEstratto { get; set; }

        [Required]
        public required string Fonte { get; set; }

        public int Occorrenze { get; set; }

        [Required]
        public required string Stato { get; set; }

        public int? IdIndirizzoNormalizzato { get; set; }

        public DateTime? DataCreazione { get; set; }

        public DateTime? DataAggiornamento { get; set; }

        [Required]
        public int IdUser { get; set; }

        public string? Note { get; set; }

        public IndirizzoNormalizzato? IndirizzoNormalizzato { get; set; }

        private static readonly List<string> tipiVieEnteValidi = new()
        {
            "Via", "Vicolo", "Largo", "Piazza", "Viale", "Corso", "Viadotto", "Strada", "Piazzetta", "Parco", "Contrada"
        };

        private static readonly List<string> fontiValide = new()
        {
            "ANAGRAFE", "UTENZE", "ANAGRAFE_SNAPSHOT", "UTENZE_SNAPSHOT"
        };

        private static readonly List<string> statiValidi = new()
        {
            "DA_ANALIZZARE", "PROPOSTA", "COLLEGATA", "AMBIGUA", "SCARTATA"
        };

        public static List<string> GetTipiVieEnteValidi()
        {
            return tipiVieEnteValidi;
        }

        public static List<string> GetFontiValide()
        {
            return fontiValide;
        }

        public static List<string> GetStatiValidi()
        {
            return statiValidi;
        }

        [NotMapped]
        public int? id
        {
            get => Id == 0 ? null : Id;
            set => Id = value ?? 0;
        }

        [NotMapped]
        public string denominazione
        {
            get => DenominazioneOriginale;
            set
            {
                DenominazioneOriginale = value;
                DenominazionePulita = string.IsNullOrWhiteSpace(DenominazionePulita) ? value : DenominazionePulita;
            }
        }

        [NotMapped]
        public string tipoVia
        {
            get => TipologiaVia;
            set => TipologiaVia = value;
        }

        [NotMapped]
        public DateTime? dataCreazione
        {
            get => DataCreazione;
            set => DataCreazione = value;
        }

        [NotMapped]
        public DateTime? dataAggiornamento
        {
            get => DataAggiornamento;
            set => DataAggiornamento = value;
        }

        [SetsRequiredMembers]
        public VieEnte()
        {
            DenominazioneOriginale = string.Empty;
            DenominazionePulita = string.Empty;
            TipologiaVia = string.Empty;
            Fonte = "UTENZE";
            Stato = "DA_ANALIZZARE";
            Occorrenze = 1;
            DataCreazione = DateTime.Now;
            DataAggiornamento = null;
        }

        [SetsRequiredMembers]
        public VieEnte(int id, string denominazione, string tipoVia, int idEnte, int idIndirizzoNormalizzato)
            : this()
        {
            Id = id;
            IdEnte = idEnte;
            DenominazioneOriginale = denominazione;
            DenominazionePulita = denominazione;
            TipologiaVia = tipoVia;
            IdIndirizzoNormalizzato = idIndirizzoNormalizzato;
        }

        public override string ToString()
        {
            return $"{DenominazioneOriginale} ({TipologiaVia}, IdEnte: {IdEnte}, IdIndirizzoNormalizzato: {IdIndirizzoNormalizzato})";
        }
    }
}
