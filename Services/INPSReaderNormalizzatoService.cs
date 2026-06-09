using Data;
using leggiCSV;
using Microsoft.EntityFrameworkCore;
using Models;

namespace BonusIdrici2.Services
{
    public class INPSReaderNormalizzatoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<INPSReaderNormalizzatoService> _logger;

        public INPSReaderNormalizzatoService(ApplicationDbContext context, ILogger<INPSReaderNormalizzatoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DatiCsvCompilati> LeggiFileINPSNormalizzatoAsync(
            string filePath,
            int selectedEnteId,
            int idReport,
            bool confrontoCivico,
            bool escludiComponenti,
            bool escludiAlertSnapshot,
            int annoReport,
            int meseReport)
        {
            var cacheVie = await CaricaCacheVieEnteAsync(selectedEnteId);
            var noteConfronto = new List<string>();

            using var confrontoNormalizzato = FunzioniTrasversali.UsaConfrontoIndirizziNormalizzato(
                (indirizzo1, civico1, indirizzo2, civico2, usaConfrontoCivico, fallback) =>
                {
                    var esito = VerificaCoerenzaIndirizziNormalizzati(
                        cacheVie,
                        indirizzo1,
                        civico1,
                        indirizzo2,
                        civico2,
                        usaConfrontoCivico,
                        fallback);

                    if (!string.IsNullOrWhiteSpace(esito.Nota))
                    {
                        noteConfronto.Add(esito.Nota);
                    }

                    return esito.Coerente;
                });

            /*
             * LOGICA LEGACY INPS - MANTENUTA COME FALLBACK
             *
             * La vecchia lettura INPS tramite CSVReader.LeggiFileINPS viene mantenuta
             * disponibile e ripristinabile rapidamente. La nuova elaborazione usa
             * questo servizio per sostituire esclusivamente il confronto indirizzi
             * tramite VieEnte e IndirizziNormalizzati.
             */
            var datiComplessivi = CSVReader.LeggiFileINPS(
                filePath,
                _context,
                selectedEnteId,
                idReport,
                confrontoCivico,
                escludiComponenti,
                annoReport,
                meseReport);

            AggiungiNoteConfronto(datiComplessivi, noteConfronto);

            _logger.LogInformation(
                "Elaborazione INPS normalizzata completata. Ente: {IdEnte}, Report: {IdReport}, Note confronto raccolte: {NoteCount}",
                selectedEnteId,
                idReport,
                noteConfronto.Count);

            return datiComplessivi;
        }

        private async Task<VieEnteLookup> CaricaCacheVieEnteAsync(int idEnte)
        {
            var vie = await _context.VieEnte
                .AsNoTracking()
                .Where(v => v.IdEnte == idEnte && v.Stato != "SCARTATA")
                .ToListAsync();

            var lookup = new VieEnteLookup();

            foreach (var via in vie)
            {
                AggiungiChiave(lookup, via.DenominazioneOriginale, via);
                AggiungiChiave(lookup, via.DenominazionePulita, via);
                AggiungiChiave(lookup, via.DenominazioneNormalizzataProposta, via);
            }

            return lookup;
        }

        private static void AggiungiChiave(VieEnteLookup lookup, string? indirizzo, VieEnte via)
        {
            var chiave = NormalizzaIndirizzoPerRicerca(indirizzo, null);

            if (string.IsNullOrWhiteSpace(chiave))
            {
                return;
            }

            if (!lookup.PerChiave.TryGetValue(chiave, out var vie))
            {
                vie = new List<VieEnte>();
                lookup.PerChiave[chiave] = vie;
            }

            vie.Add(via);
        }

        private static EsitoConfrontoIndirizzo VerificaCoerenzaIndirizziNormalizzati(
            VieEnteLookup lookup,
            string? indirizzo1,
            string? civico1,
            string? indirizzo2,
            string? civico2,
            bool confrontoCivico,
            Func<string?, string?, string?, string?, bool, bool> fallback)
        {
            if (confrontoCivico && !CiviciCoerenti(indirizzo1, civico1, indirizzo2, civico2))
            {
                return new EsitoConfrontoIndirizzo
                {
                    Coerente = false,
                    MetodoConfronto = "CIVICO",
                    Nota = "Confronto indirizzo: civico non coerente."
                };
            }

            var via1 = TrovaViaEnte(lookup, indirizzo1, civico1);
            var via2 = TrovaViaEnte(lookup, indirizzo2, civico2);
            var id1 = via1?.IdIndirizzoNormalizzato;
            var id2 = via2?.IdIndirizzoNormalizzato;

            if (id1.HasValue && id2.HasValue)
            {
                var coerente = id1.Value == id2.Value;

                return new EsitoConfrontoIndirizzo
                {
                    Coerente = coerente,
                    MetodoConfronto = "IdIndirizzoNormalizzato",
                    IdIndirizzoNormalizzatoInps = id1,
                    IdIndirizzoNormalizzatoUtenza = id2,
                    Nota = coerente
                        ? $"Confronto indirizzo tramite IdIndirizzoNormalizzato: entrambi collegati a ID {id1.Value}."
                        : $"Indirizzi collegati a IdIndirizzoNormalizzato diversi: {id1.Value} e {id2.Value}."
                };
            }

            var fallbackCoerente = fallback(indirizzo1, civico1, indirizzo2, civico2, confrontoCivico);
            var notaFallback = CreaNotaFallback(via1, via2);

            return new EsitoConfrontoIndirizzo
            {
                Coerente = fallbackCoerente,
                MetodoConfronto = "FallbackTestuale",
                IdIndirizzoNormalizzatoInps = id1,
                IdIndirizzoNormalizzatoUtenza = id2,
                Nota = notaFallback
            };
        }

        private static VieEnte? TrovaViaEnte(VieEnteLookup lookup, string? indirizzo, string? numeroCivico)
        {
            var chiave = NormalizzaIndirizzoPerRicerca(indirizzo, numeroCivico);

            if (string.IsNullOrWhiteSpace(chiave) || !lookup.PerChiave.TryGetValue(chiave, out var vie))
            {
                return null;
            }

            return vie
                .OrderByDescending(v => v.IdIndirizzoNormalizzato.HasValue)
                .ThenByDescending(v => v.Stato == "COLLEGATA")
                .ThenByDescending(v => v.Occorrenze)
                .FirstOrDefault();
        }

        private static string NormalizzaIndirizzoPerRicerca(string? indirizzo, string? numeroCivico)
        {
            var separato = FunzioniTrasversali.ExtractToponimoAndCivico(indirizzo, numeroCivico);
            return FunzioniTrasversali.NormalizeToponimo(separato.Toponimo);
        }

        private static bool CiviciCoerenti(string? indirizzo1, string? civico1, string? indirizzo2, string? civico2)
        {
            var separato1 = FunzioniTrasversali.ExtractToponimoAndCivico(indirizzo1, civico1);
            var separato2 = FunzioniTrasversali.ExtractToponimoAndCivico(indirizzo2, civico2);

            return string.Equals(separato1.NumeroCivico, separato2.NumeroCivico, StringComparison.OrdinalIgnoreCase);
        }

        private static string CreaNotaFallback(VieEnte? via1, VieEnte? via2)
        {
            if (via1 == null && via2 == null)
            {
                return "Confronto indirizzo effettuato tramite fallback testuale: normalizzazione non disponibile.";
            }

            if (via1?.IdIndirizzoNormalizzato == null || via2?.IdIndirizzoNormalizzato == null)
            {
                return "ViaEnte non collegata a IndirizzoNormalizzato: completare la normalizzazione. Confronto effettuato tramite fallback testuale.";
            }

            return "Confronto indirizzo effettuato tramite fallback testuale.";
        }

        private static void AggiungiNoteConfronto(DatiCsvCompilati datiComplessivi, List<string> noteConfronto)
        {
            var noteDistinte = noteConfronto
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            if (noteDistinte.Count == 0)
            {
                return;
            }

            var nota = string.Join("\n", noteDistinte);

            foreach (var domanda in datiComplessivi.domande.Concat(datiComplessivi.domandeDaAggiornare))
            {
                if (domanda.idUtenza == null)
                {
                    continue;
                }

                domanda.note = string.IsNullOrWhiteSpace(domanda.note)
                    ? nota
                    : domanda.note + "\n" + nota;
            }
        }

        private sealed class VieEnteLookup
        {
            public Dictionary<string, List<VieEnte>> PerChiave { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class EsitoConfrontoIndirizzo
        {
            public bool Coerente { get; set; }
            public string MetodoConfronto { get; set; } = string.Empty;
            public string? Nota { get; set; }
            public int? IdIndirizzoNormalizzatoInps { get; set; }
            public int? IdIndirizzoNormalizzatoUtenza { get; set; }
        }
    }
}
