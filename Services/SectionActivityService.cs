using Data;
using Models.ViewModels;

namespace BonusIdrici2.Services
{
    public class SectionActivityService
    {
        private readonly ApplicationDbContext _context;

        public SectionActivityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public SectionActivitySummaryViewModel GetAnagrafeActivity(int enteId)
        {
            var items = _context.Dichiaranti
                .Where(x => x.IdEnte == enteId)
                .Select(x => new
                {
                    x.id,
                    x.Cognome,
                    x.Nome,
                    x.CodiceFiscale,
                    x.data_creazione,
                    x.data_aggiornamento,
                    Timestamp = x.data_aggiornamento ?? x.data_creazione
                })
                .OrderByDescending(x => x.Timestamp)
                .Take(6)
                .ToList()
                .Select(x => new SectionActivityItemViewModel
                {
                    Timestamp = x.Timestamp,
                    Operation = x.data_aggiornamento.HasValue ? "Modifica anagrafica" : "Nuovo inserimento",
                    Title = $"{x.Cognome} {x.Nome}".Trim(),
                    Detail = string.IsNullOrWhiteSpace(x.CodiceFiscale) ? null : $"CF {x.CodiceFiscale}",
                    Icon = x.data_aggiornamento.HasValue ? "bi-pencil-square" : "bi-plus-circle",
                    Tone = x.data_aggiornamento.HasValue ? "activity-blue" : "activity-green",
                    Controller = "Anagrafe",
                    Action = "Modifica",
                    RouteId = x.id
                })
                .ToList();

            return BuildSummary("Anagrafe", items);
        }

        public SectionActivitySummaryViewModel GetToponomiActivity(int enteId)
        {
            var items = _context.Toponomi
                .Where(x => x.IdEnte == enteId)
                .Select(x => new
                {
                    x.id,
                    x.denominazione,
                    x.normalizzazione,
                    x.dataCreazione,
                    x.dataAggiornamento,
                    Timestamp = x.dataAggiornamento ?? x.dataCreazione
                })
                .OrderByDescending(x => x.Timestamp)
                .Take(6)
                .ToList()
                .Select(x => new SectionActivityItemViewModel
                {
                    Timestamp = x.Timestamp,
                    Operation = x.dataAggiornamento.HasValue ? "Modifica toponimo" : "Nuovo toponimo",
                    Title = x.denominazione,
                    Detail = string.IsNullOrWhiteSpace(x.normalizzazione) ? "Normalizzazione non presente" : $"Normalizzato: {x.normalizzazione}",
                    Icon = x.dataAggiornamento.HasValue ? "bi-pencil-square" : "bi-signpost-split",
                    Tone = x.dataAggiornamento.HasValue ? "activity-blue" : "activity-cyan",
                    Controller = "Toponomi",
                    Action = "Modifica",
                    RouteId = x.id
                })
                .ToList();

            return BuildSummary("Toponomi", items);
        }

        public SectionActivitySummaryViewModel GetUtenzeActivity(int enteId)
        {
            var items = _context.UtenzeIdriche
                .Where(x => x.IdEnte == enteId)
                .Select(x => new
                {
                    x.id,
                    x.cognome,
                    x.nome,
                    x.idAcquedotto,
                    x.matricolaContatore,
                    x.data_creazione,
                    x.data_aggiornamento,
                    Timestamp = x.data_aggiornamento ?? x.data_creazione
                })
                .OrderByDescending(x => x.Timestamp)
                .Take(6)
                .ToList()
                .Select(x => new SectionActivityItemViewModel
                {
                    Timestamp = x.Timestamp,
                    Operation = x.data_aggiornamento.HasValue ? "Modifica utenza" : "Nuova utenza",
                    Title = $"{x.cognome} {x.nome}".Trim(),
                    Detail = BuildUtenzaDetail(x.idAcquedotto, x.matricolaContatore),
                    Icon = x.data_aggiornamento.HasValue ? "bi-pencil-square" : "bi-droplet",
                    Tone = x.data_aggiornamento.HasValue ? "activity-blue" : "activity-cyan",
                    Controller = "Utenze",
                    Action = "Modifica",
                    RouteId = x.id
                })
                .ToList();

            return BuildSummary("Utenze idriche", items);
        }

        public SectionActivitySummaryViewModel GetReportsActivity(int enteId)
        {
            var items = _context.Reports
                .Where(x => x.idEnte == enteId)
                .Select(x => new
                {
                    x.id,
                    x.mese,
                    x.anno,
                    x.serie,
                    x.stato,
                    x.DataCreazione,
                    x.DataAggiornamento,
                    Timestamp = x.DataAggiornamento ?? x.DataCreazione
                })
                .OrderByDescending(x => x.Timestamp)
                .Take(6)
                .ToList()
                .Select(x => new SectionActivityItemViewModel
                {
                    Timestamp = x.Timestamp,
                    Operation = x.DataAggiornamento.HasValue ? "Aggiornamento elaborazione" : "Nuova elaborazione",
                    Title = $"{x.mese} {x.anno} - Serie {x.serie}",
                    Detail = string.IsNullOrWhiteSpace(x.stato) ? null : $"Stato: {x.stato}",
                    Icon = x.DataAggiornamento.HasValue ? "bi-pencil-square" : "bi-file-earmark-bar-graph",
                    Tone = x.DataAggiornamento.HasValue ? "activity-blue" : "activity-green",
                    Controller = "Report",
                    Action = "Dettails",
                    RouteId = x.id,
                    RouteParameterName = "idReport"
                })
                .ToList();

            return BuildSummary("Elaborazioni", items);
        }

        public AdminActivityDashboardViewModel GetAdminActivityDashboard()
        {
            return new AdminActivityDashboardViewModel
            {
                Sections = new List<SectionActivitySummaryViewModel>
                {
                    GetAnagrafeActivity(),
                    GetToponomiActivity(),
                    GetUtenzeActivity(),
                    GetReportsActivity()
                },
                GeneratedAt = DateTime.Now
            };
        }

        private SectionActivitySummaryViewModel GetAnagrafeActivity()
        {
            var enti = _context.Enti.ToDictionary(x => x.id, x => x.nome);
            var items = _context.Dichiaranti
                .Select(x => new
                {
                    x.id,
                    x.Cognome,
                    x.Nome,
                    x.CodiceFiscale,
                    x.IdEnte,
                    x.data_creazione,
                    x.data_aggiornamento,
                    Timestamp = x.data_aggiornamento ?? x.data_creazione
                })
                .OrderByDescending(x => x.Timestamp)
                .Take(12)
                .ToList()
                .Select(x => new SectionActivityItemViewModel
                {
                    Timestamp = x.Timestamp,
                    Operation = x.data_aggiornamento.HasValue ? "Modifica anagrafica" : "Nuovo inserimento",
                    Title = $"{x.Cognome} {x.Nome}".Trim(),
                    Detail = BuildAdminDetail(enti, x.IdEnte, string.IsNullOrWhiteSpace(x.CodiceFiscale) ? null : $"CF {x.CodiceFiscale}"),
                    Icon = x.data_aggiornamento.HasValue ? "bi-pencil-square" : "bi-plus-circle",
                    Tone = x.data_aggiornamento.HasValue ? "activity-blue" : "activity-green",
                    Controller = "Anagrafe",
                    Action = "Modifica",
                    RouteId = x.id
                })
                .ToList();

            return BuildSummary("Anagrafe", items);
        }

        private SectionActivitySummaryViewModel GetToponomiActivity()
        {
            var enti = _context.Enti.ToDictionary(x => x.id, x => x.nome);
            var items = _context.Toponomi
                .Select(x => new
                {
                    x.id,
                    x.denominazione,
                    x.normalizzazione,
                    x.IdEnte,
                    x.dataCreazione,
                    x.dataAggiornamento,
                    Timestamp = x.dataAggiornamento ?? x.dataCreazione
                })
                .Where(x => x.Timestamp != null)
                .OrderByDescending(x => x.Timestamp)
                .Take(12)
                .ToList()
                .Select(x => new SectionActivityItemViewModel
                {
                    Timestamp = x.Timestamp,
                    Operation = x.dataAggiornamento.HasValue ? "Modifica toponimo" : "Nuovo toponimo",
                    Title = x.denominazione,
                    Detail = BuildAdminDetail(enti, x.IdEnte, string.IsNullOrWhiteSpace(x.normalizzazione) ? "Normalizzazione non presente" : $"Normalizzato: {x.normalizzazione}"),
                    Icon = x.dataAggiornamento.HasValue ? "bi-pencil-square" : "bi-signpost-split",
                    Tone = x.dataAggiornamento.HasValue ? "activity-blue" : "activity-cyan",
                    Controller = "Toponomi",
                    Action = "Modifica",
                    RouteId = x.id
                })
                .ToList();

            return BuildSummary("Toponomi", items);
        }

        private SectionActivitySummaryViewModel GetUtenzeActivity()
        {
            var enti = _context.Enti.ToDictionary(x => x.id, x => x.nome);
            var items = _context.UtenzeIdriche
                .Select(x => new
                {
                    x.id,
                    x.cognome,
                    x.nome,
                    x.idAcquedotto,
                    x.matricolaContatore,
                    x.IdEnte,
                    x.data_creazione,
                    x.data_aggiornamento,
                    Timestamp = x.data_aggiornamento ?? x.data_creazione
                })
                .Where(x => x.Timestamp != null)
                .OrderByDescending(x => x.Timestamp)
                .Take(12)
                .ToList()
                .Select(x => new SectionActivityItemViewModel
                {
                    Timestamp = x.Timestamp,
                    Operation = x.data_aggiornamento.HasValue ? "Modifica utenza" : "Nuova utenza",
                    Title = $"{x.cognome} {x.nome}".Trim(),
                    Detail = BuildAdminDetail(enti, x.IdEnte, BuildUtenzaDetail(x.idAcquedotto, x.matricolaContatore)),
                    Icon = x.data_aggiornamento.HasValue ? "bi-pencil-square" : "bi-droplet",
                    Tone = x.data_aggiornamento.HasValue ? "activity-blue" : "activity-cyan",
                    Controller = "Utenze",
                    Action = "Modifica",
                    RouteId = x.id
                })
                .ToList();

            return BuildSummary("Utenze idriche", items);
        }

        private SectionActivitySummaryViewModel GetReportsActivity()
        {
            var enti = _context.Enti.ToDictionary(x => x.id, x => x.nome);
            var items = _context.Reports
                .Select(x => new
                {
                    x.id,
                    x.mese,
                    x.anno,
                    x.serie,
                    x.stato,
                    x.idEnte,
                    x.DataCreazione,
                    x.DataAggiornamento,
                    Timestamp = x.DataAggiornamento ?? x.DataCreazione
                })
                .OrderByDescending(x => x.Timestamp)
                .Take(12)
                .ToList()
                .Select(x => new SectionActivityItemViewModel
                {
                    Timestamp = x.Timestamp,
                    Operation = x.DataAggiornamento.HasValue ? "Aggiornamento elaborazione" : "Nuova elaborazione",
                    Title = $"{x.mese} {x.anno} - Serie {x.serie}",
                    Detail = BuildAdminDetail(enti, x.idEnte, string.IsNullOrWhiteSpace(x.stato) ? null : $"Stato: {x.stato}"),
                    Icon = x.DataAggiornamento.HasValue ? "bi-pencil-square" : "bi-file-earmark-bar-graph",
                    Tone = x.DataAggiornamento.HasValue ? "activity-blue" : "activity-green",
                    Controller = "Report",
                    Action = "Dettails",
                    RouteId = x.id,
                    RouteParameterName = "idReport"
                })
                .ToList();

            return BuildSummary("Elaborazioni", items);
        }

        private static SectionActivitySummaryViewModel BuildSummary(string sectionName, List<SectionActivityItemViewModel> items)
        {
            return new SectionActivitySummaryViewModel
            {
                SectionName = sectionName,
                LastUpdate = items.Count == 0 ? null : items.Max(x => x.Timestamp),
                RecentActivities = items
            };
        }

        private static string? BuildUtenzaDetail(string? idAcquedotto, string? matricolaContatore)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(idAcquedotto))
            {
                parts.Add($"Acquedotto {idAcquedotto}");
            }

            if (!string.IsNullOrWhiteSpace(matricolaContatore))
            {
                parts.Add($"Matricola {matricolaContatore}");
            }

            return parts.Count == 0 ? null : string.Join(" - ", parts);
        }

        private static string BuildAdminDetail(Dictionary<int, string> enti, int enteId, string? detail)
        {
            var ente = enti.TryGetValue(enteId, out var nomeEnte) ? nomeEnte : $"Ente ID {enteId}";

            return string.IsNullOrWhiteSpace(detail)
                ? ente
                : $"{ente} - {detail}";
        }
    }
}
