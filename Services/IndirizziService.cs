using Models;
using Data;
using System.Text.RegularExpressions;

namespace BonusIdrici2.Services
{
    public class IndirizziService
    {

        private readonly ApplicationDbContext _context;
        private static readonly (string TipoVia, string[] Patterns)[] TipiViaPatterns =
        {
            ("Contrada", new[] { @"^C\s*[./]?\s*DA\b", @"^CDA\b", @"^CONTRADA\b", @"^CONTR\.\b" }),
            ("Piazzetta", new[] { @"^PIAZZETTA\b", @"^P\.?\s*ZZETTA\b" }),
            ("Piazza", new[] { @"^PIAZZA\b", @"^P\.?\s*ZZA\b", @"^PZA\b", @"^PZZA\b" }),
            ("Viale", new[] { @"^VIALE\b", @"^V\.?\s*LE\b", @"^VLE\b" }),
            ("Vicolo", new[] { @"^VICOLO\b", @"^VIC\.\b", @"^VIC\b" }),
            ("Largo", new[] { @"^LARGO\b", @"^LGO\b", @"^L\.?\s*GO\b" }),
            ("Corso", new[] { @"^CORSO\b", @"^C\.?\s*SO\b", @"^CSO\b" }),
            ("Viadotto", new[] { @"^VIADOTTO\b" }),
            ("Strada", new[] { @"^STRADA\b", @"^STR\.\b", @"^STR\b" }),
            ("Parco", new[] { @"^PARCO\b" }),
            ("Via", new[] { @"^VIA\b", @"^V\.\s*", @"^V\b" })
        };

        public IndirizziService (ApplicationDbContext context)
        {
            _context = context;
        }

        public List<VieEnte> RicavaViePerEnte(int idEnte)
        {

            // 1. Interrogo il database per ottenere tutte le vie partendo dall'anagrafe dell'ente

            var vieEnteAnagrafe = _context.Dichiaranti.Where(d => d.IdEnte == idEnte)
                .Select(d => d.IndirizzoResidenza)
                .ToList()
                .Select(indirizzo => CreaViaEnte(indirizzo, idEnte));

           // 2. Interrogo il DB per ottenere tutte le vie delle utenze idriche collegate all'ente

            var vieEnteUtenzeIdriche = _context.UtenzeIdriche.Where(u => u.IdEnte == idEnte)
                .Select(u => u.indirizzoUbicazione)
                .ToList()
                .Select(indirizzo => CreaViaEnte(indirizzo, idEnte));

            // 3. Unisco le due liste e rimuovo eventuali duplicati

            var vieUnificate = vieEnteAnagrafe.Union(vieEnteUtenzeIdriche, new VieEnteComparer()).ToList();

            return vieUnificate;
            
        }

        public static string RicavaTipoVia(string? denominazione)
        {
            if (string.IsNullOrWhiteSpace(denominazione))
            {
                return "Via";
            }

            var indirizzoNormalizzato = NormalizzaTesto(denominazione);

            foreach (var tipoViaPattern in TipiViaPatterns)
            {
                if (tipoViaPattern.Patterns.Any(pattern => Regex.IsMatch(indirizzoNormalizzato, pattern, RegexOptions.IgnoreCase)))
                {
                    return tipoViaPattern.TipoVia;
                }
            }

            return "Via";
        }

        private static VieEnte CreaViaEnte(string? indirizzo, int idEnte)
        {
            var denominazione = string.IsNullOrWhiteSpace(indirizzo) ? string.Empty : indirizzo.Trim();

            return new VieEnte
            {
                DenominazioneOriginale = denominazione,
                DenominazionePulita = denominazione,
                DenominazioneNormalizzataProposta = NormalizzaTesto(denominazione),
                TipologiaVia = RicavaTipoVia(denominazione),
                IdEnte = idEnte,
                IdIndirizzoNormalizzato = null,
                Fonte = "UTENZE",
                Occorrenze = 1,
                Stato = "DA_ANALIZZARE",
                DataCreazione = DateTime.Now,
                DataAggiornamento = null,
                IdUser = 0
            };
        }

        private static string NormalizzaTesto(string testo)
        {
            return Regex.Replace(testo.Trim().ToUpperInvariant(), @"\s+", " ");
        }
    }

    public class VieEnteComparer : IEqualityComparer<VieEnte>
    {
        public bool Equals(VieEnte? x, VieEnte? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.IdEnte == y.IdEnte
                && string.Equals(x.tipoVia, y.tipoVia, StringComparison.OrdinalIgnoreCase)
                && string.Equals(NormalizzaDenominazione(x.denominazione), NormalizzaDenominazione(y.denominazione), StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(VieEnte obj)
        {
            return HashCode.Combine(
                obj.IdEnte,
                obj.tipoVia.ToUpperInvariant(),
                NormalizzaDenominazione(obj.denominazione).ToUpperInvariant());
        }

        private static string NormalizzaDenominazione(string? denominazione)
        {
            if (string.IsNullOrWhiteSpace(denominazione))
            {
                return string.Empty;
            }

            return Regex.Replace(denominazione.Trim(), @"\s+", " ");
        }
    }
}
