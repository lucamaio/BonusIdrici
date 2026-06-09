using Data;
using Models;
using Models.ViewModels;

namespace BonusIdrici2.Services
{
    public class SectionActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private readonly List<(string Key, string Title, string Path)> logDefinitions = new()
        {
            ("Accessi", "Accessi utenti", "wwwroot/log/utenti.log"),
            ("Esportazioni", "Elaborazioni INPS", "wwwroot/log/Elaborazione_INPS.log"),
            ("CaricamentoUtenze", "Caricamento utenze", "wwwroot/log/Elaborazione_Utenze.log"),
            ("CaricamentoAnagrafiche", "Caricamento anagrafe", "wwwroot/log/Elaborazione_Anagrafe.log"),
            ("IndirizziNormalizzati", "Indirizzi normalizzati", "wwwroot/log/IndirizziNormalizzati.log"),
            ("Domande", "Domande e report", "wwwroot/log/Domande.log"),
            ("Report", "Report applicativi", "wwwroot/log/Report.log")
        };

        public SectionActivityService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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

        public SectionActivitySummaryViewModel GetIndirizziNormalizzatiActivity(int enteId)
        {
            var items = _context.IndirizziNormalizzati
                .Where(x => x.IdEnte == enteId)
                .Select(x => new
                {
                    x.Id,
                    x.DenominazioneNormalizzata,
                    x.Attivo,
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
                    Operation = x.DataAggiornamento.HasValue ? "Modifica indirizzo" : "Nuovo indirizzo",
                    Title = x.DenominazioneNormalizzata,
                    Detail = x.Attivo ? "Verificato" : "Da verificare",
                    Icon = x.DataAggiornamento.HasValue ? "bi-pencil-square" : "bi-link-45deg",
                    Tone = x.DataAggiornamento.HasValue ? "activity-blue" : "activity-green",
                    Controller = "IndirizziNormalizzati",
                    Action = "Modifica",
                    RouteId = x.Id
                })
                .ToList();

            return BuildSummary("Indirizzi normalizzati", items);
        }

        public AdminActivityDashboardViewModel GetAdminActivityDashboard()
        {
            return new AdminActivityDashboardViewModel
            {
                Sections = new List<SectionActivitySummaryViewModel>
                {
                    GetAdminAnagrafeActivity(5),
                    GetAdminToponomiActivity(5),
                    GetAdminUtenzeActivity(5),
                    GetAdminReportsActivity(5),
                    GetAdminIndirizziNormalizzatiActivity(5)
                },
                DiagnosticActivities = GetDiagnosticActivityCategories(),
                GeneratedAt = DateTime.Now
            };
        }

        public AdminActivityDetailViewModel? GetAdminActivityDetail(string sectionKey)
        {
            var section = sectionKey?.Trim().ToLowerInvariant() switch
            {
                "anagrafe" => GetAdminAnagrafeActivity(),
                "toponomi" => GetAdminToponomiActivity(),
                "utenze" => GetAdminUtenzeActivity(),
                "elaborazioni" => GetAdminReportsActivity(),
                "indirizzi-normalizzati" => GetAdminIndirizziNormalizzatiActivity(),
                _ => null
            };

            if (section == null)
            {
                return null;
            }

            return new AdminActivityDetailViewModel
            {
                SectionKey = section.SectionKey,
                SectionName = section.SectionName,
                LastUpdate = section.LastUpdate,
                Activities = section.RecentActivities
            };
        }

        public IReadOnlyList<DiagnosticActivityCategoryViewModel> GetDiagnosticActivityCategories(int recentItemsPerLevel = 5)
        {
            var logs = ReadDiagnosticLogs();
            var categories = new[]
            {
                new { Level = "ERROR", Title = "Error", Icon = "bi-x-octagon-fill", Tone = "activity-red" },
                new { Level = "WARNING", Title = "Warning", Icon = "bi-exclamation-triangle-fill", Tone = "activity-amber" },
                new { Level = "INFO", Title = "Info", Icon = "bi-info-circle-fill", Tone = "activity-blue" },
                new { Level = "DEBUG", Title = "Debug", Icon = "bi-bug-fill", Tone = "activity-cyan" }
            };

            return categories
                .Select(category =>
                {
                    var categoryLogs = logs
                        .Where(log => string.Equals(log.Level, category.Level, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(log => log.Timestamp)
                        .ToList();

                    return new DiagnosticActivityCategoryViewModel
                    {
                        Level = category.Level,
                        Title = category.Title,
                        Icon = category.Icon,
                        Tone = category.Tone,
                        TotalCount = categoryLogs.Count,
                        RecentEvents = categoryLogs
                            .Take(recentItemsPerLevel)
                            .Select(log => new DiagnosticActivityItemViewModel
                            {
                                Timestamp = log.Timestamp,
                                Message = log.Message,
                                SourceKey = log.SourceKey,
                                SourceTitle = log.SourceTitle
                            })
                            .ToList()
                    };
                })
                .ToList();
        }

        private SectionActivitySummaryViewModel GetAdminAnagrafeActivity(int? take = null)
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
                .Take(take ?? int.MaxValue)
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

            return BuildSummary("Anagrafe", items, "anagrafe");
        }

        private SectionActivitySummaryViewModel GetAdminToponomiActivity(int? take = null)
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
                .Take(take ?? int.MaxValue)
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

            return BuildSummary("Toponomi", items, "toponomi");
        }

        private SectionActivitySummaryViewModel GetAdminUtenzeActivity(int? take = null)
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
                .Take(take ?? int.MaxValue)
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

            return BuildSummary("Utenze idriche", items, "utenze");
        }

        private SectionActivitySummaryViewModel GetAdminReportsActivity(int? take = null)
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
                .Take(take ?? int.MaxValue)
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

            return BuildSummary("Elaborazioni", items, "elaborazioni");
        }

        private SectionActivitySummaryViewModel GetAdminIndirizziNormalizzatiActivity(int? take = null)
        {
            var enti = _context.Enti.ToDictionary(x => x.id, x => x.nome);
            var items = _context.IndirizziNormalizzati
                .Select(x => new
                {
                    x.Id,
                    x.DenominazioneNormalizzata,
                    x.IdEnte,
                    x.Attivo,
                    x.DataCreazione,
                    x.DataAggiornamento,
                    Timestamp = x.DataAggiornamento ?? x.DataCreazione
                })
                .Where(x => x.Timestamp != null)
                .OrderByDescending(x => x.Timestamp)
                .Take(take ?? int.MaxValue)
                .ToList()
                .Select(x => new SectionActivityItemViewModel
                {
                    Timestamp = x.Timestamp,
                    Operation = x.DataAggiornamento.HasValue ? "Modifica indirizzo" : "Nuovo indirizzo",
                    Title = x.DenominazioneNormalizzata,
                    Detail = BuildAdminDetail(enti, x.IdEnte, x.Attivo ? "Verificato" : "Da verificare"),
                    Icon = x.DataAggiornamento.HasValue ? "bi-pencil-square" : "bi-link-45deg",
                    Tone = x.DataAggiornamento.HasValue ? "activity-blue" : "activity-green",
                    Controller = "IndirizziNormalizzati",
                    Action = "Modifica",
                    RouteId = x.Id
                })
                .ToList();

            return BuildSummary("Indirizzi normalizzati", items, "indirizzi-normalizzati");
        }

        private static SectionActivitySummaryViewModel BuildSummary(string sectionName, List<SectionActivityItemViewModel> items, string sectionKey = "")
        {
            return new SectionActivitySummaryViewModel
            {
                SectionName = sectionName,
                SectionKey = sectionKey,
                LastUpdate = items.Count == 0 ? null : items.Max(x => x.Timestamp),
                TotalCount = items.Count,
                RecentActivities = items,
                DiagnosticActivities = new List<DiagnosticActivityCategoryViewModel>()
            };
        }

        private List<(DateTime Timestamp, string Level, string Message, string SourceKey, string SourceTitle)> ReadDiagnosticLogs()
        {
            var logs = new List<(DateTime Timestamp, string Level, string Message, string SourceKey, string SourceTitle)>();

            foreach (var definition in logDefinitions)
            {
                var fullPath = ResolvePath(definition.Path);

                if (!File.Exists(fullPath))
                {
                    continue;
                }

                using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);

                while (reader.ReadLine() is { } line)
                {
                    try
                    {
                        var log = new Log(line);
                        logs.Add((log.Timestamp, log.TipoLog.ToUpperInvariant(), log.Messaggio, definition.Key, definition.Title));
                    }
                    catch
                    {
                        // Le righe non conformi non devono bloccare il riepilogo delle attivita.
                    }
                }
            }

            return logs;
        }

        private string ResolvePath(string path)
        {
            return Path.IsPathRooted(path)
                ? path
                : Path.Combine(_environment.ContentRootPath, path);
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
