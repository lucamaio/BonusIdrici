using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace BonusIdrici2.Services
{
    public class VieEnteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VieEnteService> _logger;

        public VieEnteService(ApplicationDbContext context, ILogger<VieEnteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PopolaVieEnteResult> PopolaVieEnteAsync(
            int idEnte,
            int idUser,
            bool includiAnagrafe = true,
            bool includiUtenze = true,
            bool includiSnapshot = false)
        {
            var result = new PopolaVieEnteResult();
            var candidati = new List<ViaEnteCandidato>();

            if (includiAnagrafe)
            {
                var indirizziAnagrafe = await _context.Dichiaranti
                    .AsNoTracking()
                    .Where(d => d.IdEnte == idEnte)
                    .Select(d => d.IndirizzoResidenza)
                    .ToListAsync();

                candidati.AddRange(indirizziAnagrafe.Select(indirizzo => new ViaEnteCandidato(indirizzo, "ANAGRAFE")));
            }

            if (includiUtenze)
            {
                var indirizziUtenze = await _context.UtenzeIdriche
                    .AsNoTracking()
                    .Where(u => u.IdEnte == idEnte)
                    .Select(u => u.indirizzoUbicazione)
                    .ToListAsync();

                candidati.AddRange(indirizziUtenze.Select(indirizzo => new ViaEnteCandidato(indirizzo, "UTENZE")));
            }

            if (includiSnapshot)
            {
                var indirizziAnagrafeSnapshot = await _context.DichiarantiSnapshot
                    .AsNoTracking()
                    .Where(d => d.IdEnte == idEnte)
                    .Select(d => d.IndirizzoResidenza)
                    .ToListAsync();

                candidati.AddRange(indirizziAnagrafeSnapshot.Select(indirizzo => new ViaEnteCandidato(indirizzo, "ANAGRAFE_SNAPSHOT")));

                var indirizziUtenzeSnapshot = await _context.UtenzeIdricheSnapshot
                    .AsNoTracking()
                    .Where(u => u.IdEnte == idEnte)
                    .Select(u => u.IndirizzoUbicazione)
                    .ToListAsync();

                candidati.AddRange(indirizziUtenzeSnapshot.Select(indirizzo => new ViaEnteCandidato(indirizzo, "UTENZE_SNAPSHOT")));
            }

            result.TotaleAnalizzate = candidati.Count;

            var gruppi = candidati
                .Select(candidato => CreaViaEnte(idEnte, idUser, candidato.Indirizzo, candidato.Fonte))
                .Where(via =>
                {
                    if (!string.IsNullOrWhiteSpace(via.DenominazionePulita))
                    {
                        return true;
                    }

                    result.Scartate++;
                    return false;
                })
                .GroupBy(via => new { via.DenominazioneOriginale, via.Fonte })
                .Select(gruppo =>
                {
                    var via = gruppo.First();
                    via.Occorrenze = gruppo.Count();
                    return via;
                })
                .ToList();

            var esistenti = await _context.VieEnte
                .Where(v => v.IdEnte == idEnte)
                .ToListAsync();

            foreach (var via in gruppi)
            {
                var esistente = esistenti.FirstOrDefault(v =>
                    string.Equals(NormalizzaChiave(v.DenominazioneOriginale), NormalizzaChiave(via.DenominazioneOriginale), StringComparison.OrdinalIgnoreCase)
                    && string.Equals(v.Fonte, via.Fonte, StringComparison.OrdinalIgnoreCase));

                if (esistente == null)
                {
                    _context.VieEnte.Add(via);
                    result.Nuove++;
                    continue;
                }

                esistente.Occorrenze += via.Occorrenze;
                esistente.DenominazionePulita = via.DenominazionePulita;
                esistente.DenominazioneNormalizzataProposta = via.DenominazioneNormalizzataProposta;
                esistente.TipologiaVia = via.TipologiaVia;
                esistente.CivicoEstratto = via.CivicoEstratto;
                esistente.DataAggiornamento = DateTime.Now;
                esistente.Note = UnisciNote(esistente.Note, via.Note);
                result.Aggiornate++;
            }

            result.GiaPresenti = esistenti.Count;
            result.ConCivicoEstratto = gruppi.Count(v => !string.IsNullOrWhiteSpace(v.CivicoEstratto));
            result.DaAnalizzare = result.Nuove + result.Aggiornate;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Popolate VieEnte per ente {IdEnte}. Nuove: {Nuove}, Aggiornate: {Aggiornate}", idEnte, result.Nuove, result.Aggiornate);

            return result;
        }

        public async Task<CreaIndirizziNormalizzatiResult> CreaIndirizziNormalizzatiAsync(
            int idEnte,
            int idUser,
            bool confermaAutomaticaSoloCasiSicuri = true)
        {
            var result = new CreaIndirizziNormalizzatiResult();

            var vie = await _context.VieEnte
                .Where(v => v.IdEnte == idEnte
                    && v.IdIndirizzoNormalizzato == null
                    && (v.Stato == "DA_ANALIZZARE" || v.Stato == "PROPOSTA"))
                .ToListAsync();

            var gruppi = vie
                .GroupBy(v => NormalizzaChiave(v.DenominazioneNormalizzataProposta ?? v.DenominazionePulita))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToList();

            result.GruppiAnalizzati = gruppi.Count;

            var indirizziEsistenti = await _context.IndirizziNormalizzati
                .Where(i => i.IdEnte == idEnte)
                .ToListAsync();

            foreach (var gruppo in gruppi)
            {
                var vieGruppo = gruppo.ToList();

                if (confermaAutomaticaSoloCasiSicuri && vieGruppo.Any(v => ContieneAbbreviazioneAmbigua(v.DenominazionePulita)))
                {
                    foreach (var via in vieGruppo)
                    {
                        via.Stato = "AMBIGUA";
                        via.Note = UnisciNote(via.Note, "Abbreviazione non collegata automaticamente per possibili corrispondenze multiple.");
                        via.DataAggiornamento = DateTime.Now;
                    }

                    result.VieAmbigue += vieGruppo.Count;
                    continue;
                }

                var denominazioneNormalizzata = gruppo.Key;
                var indirizzo = indirizziEsistenti.FirstOrDefault(i =>
                    string.Equals(NormalizzaChiave(i.DenominazioneNormalizzata), denominazioneNormalizzata, StringComparison.OrdinalIgnoreCase));

                if (indirizzo == null)
                {
                    indirizzo = new IndirizzoNormalizzato(denominazioneNormalizzata, idEnte, idUser, "Creato automaticamente da VieEnte.");
                    _context.IndirizziNormalizzati.Add(indirizzo);
                    indirizziEsistenti.Add(indirizzo);
                    result.IndirizziCreati++;
                }
                else
                {
                    result.GiaEsistenti++;
                }

                await _context.SaveChangesAsync();

                foreach (var via in vieGruppo)
                {
                    via.IdIndirizzoNormalizzato = indirizzo.Id;
                    via.Stato = "COLLEGATA";
                    via.DataAggiornamento = DateTime.Now;
                    via.Note = UnisciNote(via.Note, "Collegata automaticamente a IndirizzoNormalizzato.");
                }

                result.VieCollegate += vieGruppo.Count;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Creati indirizzi normalizzati per ente {IdEnte}. Indirizzi: {Indirizzi}, Vie collegate: {Vie}", idEnte, result.IndirizziCreati, result.VieCollegate);

            return result;
        }

        private static VieEnte CreaViaEnte(int idEnte, int idUser, string? indirizzo, string fonte)
        {
            var denominazioneOriginale = PulisciSpazi(indirizzo);
            var indirizzoSeparato = FunzioniTrasversali.ExtractToponimoAndCivico(denominazioneOriginale);
            var denominazionePulita = PulisciSpazi(indirizzoSeparato.Toponimo);
            var civicoEstratto = indirizzoSeparato.CivicoEstratto;

            return new VieEnte
            {
                IdEnte = idEnte,
                DenominazioneOriginale = denominazioneOriginale,
                DenominazionePulita = denominazionePulita,
                DenominazioneNormalizzataProposta = NormalizzaChiave(denominazionePulita),
                TipologiaVia = IndirizziService.RicavaTipoVia(denominazionePulita),
                CivicoEstratto = civicoEstratto,
                Fonte = fonte,
                Occorrenze = 1,
                Stato = "DA_ANALIZZARE",
                DataCreazione = DateTime.Now,
                DataAggiornamento = null,
                IdUser = idUser,
                Note = string.IsNullOrWhiteSpace(civicoEstratto) ? null : "Civico rilevato nel campo indirizzo."
            };
        }

        private static string PulisciSpazi(string? valore)
        {
            return string.Join(" ", (valore ?? string.Empty).Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static string NormalizzaChiave(string? valore)
        {
            return PulisciSpazi((valore ?? string.Empty).Replace(".", " "));
        }

        private static bool ContieneAbbreviazioneAmbigua(string? denominazione)
        {
            var tokens = PulisciSpazi(denominazione).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return tokens.Any(token => token.Length == 1 && char.IsLetter(token[0]));
        }

        private static string? UnisciNote(string? noteEsistenti, string? nuovaNota)
        {
            if (string.IsNullOrWhiteSpace(nuovaNota))
            {
                return noteEsistenti;
            }

            if (string.IsNullOrWhiteSpace(noteEsistenti))
            {
                return nuovaNota;
            }

            if (noteEsistenti.Contains(nuovaNota, StringComparison.OrdinalIgnoreCase))
            {
                return noteEsistenti;
            }

            return $"{noteEsistenti} {nuovaNota}";
        }

        private sealed record ViaEnteCandidato(string? Indirizzo, string Fonte);
    }

    public class PopolaVieEnteResult
    {
        public int TotaleAnalizzate { get; set; }
        public int Nuove { get; set; }
        public int Aggiornate { get; set; }
        public int GiaPresenti { get; set; }
        public int Scartate { get; set; }
        public int ConCivicoEstratto { get; set; }
        public int DaAnalizzare { get; set; }
    }

    public class CreaIndirizziNormalizzatiResult
    {
        public int GruppiAnalizzati { get; set; }
        public int IndirizziCreati { get; set; }
        public int VieCollegate { get; set; }
        public int VieAmbigue { get; set; }
        public int VieScartate { get; set; }
        public int GiaEsistenti { get; set; }
    }
}
