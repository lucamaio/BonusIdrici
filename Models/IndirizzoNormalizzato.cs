using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Models
{
    public class IndirizzoNormalizzato
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IdEnte { get; set; }

        [Required]
        public required string DenominazioneNormalizzata { get; set; }

        public DateTime? DataCreazione { get; set; }

        public DateTime? DataAggiornamento { get; set; }

        [Required]
        public int IdUser { get; set; }

        public bool Attivo { get; set; }

        public string? Note { get; set; }

        public List<VieEnte> VieEnte { get; set; } = new();

        [NotMapped]
        public int? id
        {
            get => Id == 0 ? null : Id;
            set => Id = value ?? 0;
        }

        [NotMapped]
        public string denominazione
        {
            get => DenominazioneNormalizzata;
            set => DenominazioneNormalizzata = value;
        }

        [NotMapped]
        public string stato
        {
            get => Attivo ? "Verificato" : "Da verificare";
            set => Attivo = string.Equals(value, "Verificato", StringComparison.OrdinalIgnoreCase);
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
        public IndirizzoNormalizzato()
        {
            DenominazioneNormalizzata = string.Empty;
            Attivo = true;
            DataCreazione = DateTime.Now;
            DataAggiornamento = null;
        }

        [SetsRequiredMembers]
        public IndirizzoNormalizzato(string denominazioneNormalizzata, int idEnte, int idUser, string? note = null)
        {
            IdEnte = idEnte;
            DenominazioneNormalizzata = denominazioneNormalizzata;
            IdUser = idUser;
            Attivo = true;
            Note = note;
            DataCreazione = DateTime.Now;
            DataAggiornamento = null;
        }

        [SetsRequiredMembers]
        public IndirizzoNormalizzato(string denominazioneNormalizzata, string stato)
            : this()
        {
            DenominazioneNormalizzata = denominazioneNormalizzata;
            this.stato = stato;
        }

        [SetsRequiredMembers]
        public IndirizzoNormalizzato(int id, string denominazione, string stato)
            : this(denominazione, stato)
        {
            Id = id;
        }
    }
}
