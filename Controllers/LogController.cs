using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Models; 
using Data;
using System.IO;
using Models.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Linq;  // Necessario per l'uso di ToList() e Reverse()


namespace Controllers
{
    public class LogController : Controller
    {
        // Dichiarazione delle variabili di istanza
        private readonly ILogger<LogController> _logger;
        private readonly ApplicationDbContext _context;

        private string? ruolo;
        private int? idUser;
        private string? username;

        private readonly List<(string Key, string Title, string Description, string Icon, string AccentClass, string Path)> logDefinitions = new()
        {
            ("Accessi", "Accessi utenti", "Login, logout e tentativi di accesso al sistema.", "bi-person-check", "diag-blue", "wwwroot/log/utenti.log"),
            ("Esportazioni", "Elaborazioni INPS", "Operazioni di elaborazione file INPS e generazione domande.", "bi-file-earmark-bar-graph", "diag-red", "wwwroot/log/Elaborazione_INPS.log"),
            ("CaricamentoUtenze", "Caricamento utenze", "Importazione e controllo dei flussi utenze idriche.", "bi-droplet-half", "diag-cyan", "wwwroot/log/Elaborazione_Utenze.log"),
            ("CaricamentoAnagrafiche", "Caricamento anagrafe", "Importazione e aggiornamento dati anagrafici.", "bi-people", "diag-green", "wwwroot/log/Elaborazione_Anagrafe.log"),
            ("Domande", "Domande e report", "Operazioni sulle domande elaborate e sui report.", "bi-journal-check", "diag-purple", "wwwroot/log/Domande.log"),
            ("Report", "Report applicativi", "Tracciamento tecnico dei report e delle esportazioni.", "bi-clipboard-data", "diag-amber", "wwwroot/log/Report.log")
        };

        // Costruttore

        public LogController(ILogger<LogController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;

            if (VerificaSessione())
            {
                username = HttpContext.Session.GetString("Username");
                ruolo = HttpContext.Session.GetString("Role");
                idUser = HttpContext.Session.GetInt32("idUser");
            }
        }

        // Funzione che inizializza le variabili con i dati della sessione

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Ora HttpContext è disponibile
            username = HttpContext.Session.GetString("Username");
            ruolo = HttpContext.Session.GetString("Role");
            idUser = HttpContext.Session.GetInt32("idUser") ?? 0;

            if (!VerificaSessione())
            {
                username = null;
                ruolo = null;
                idUser = null;
            }

            // Così le variabili sono disponibili in tutte le viste
            ViewBag.idUser = idUser;
            ViewBag.Username = username;
            ViewBag.Ruolo = ruolo;
        }

        // Funzione che verifica se esiste una funzione ed il ruolo e quello richiesto per accedere alla pagina

        public bool VerificaSessione(string? ruoloRichiesto = null)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(ruolo))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(ruoloRichiesto) && ruolo != ruoloRichiesto)
            {
                return false;
            }

            return true;
        }

        // Inizio - Pagine di Navigazione

        // Pagina 1 - Selezione del tipo di log da visualizzare

        public IActionResult Index()
        {
            if (!VerificaSessione("ADMIN"))
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di visualizzazione log.");
                ViewBag.Messaggio = "Accesso non autorizzato.";
                return RedirectToAction("Index", "Home");
            }
            return View(BuildDiagnostics());
        }

        // Pagina 2 - Visualizzazione del log delle operazioni

        public IActionResult Show(string tipoLog)
        {
            if (!VerificaSessione("ADMIN"))
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di visualizzazione log.");
                ViewBag.Messaggio = "Accesso non autorizzato.";
                return RedirectToAction("Index", "Home");
            }

            var logDefinition = logDefinitions.FirstOrDefault(l => l.Key == tipoLog);

            if (string.IsNullOrEmpty(tipoLog) || string.IsNullOrWhiteSpace(logDefinition.Key))
            {
                ViewBag.Messaggio = "Tipologia di log non valida.";
                return RedirectToAction("Index");
            }

            var righeFile = ReadLogLines(logDefinition.Path);

            if (righeFile == null || righeFile.Count == 0)
            {
                ViewBag.Messaggio = "Nessun log disponibile per la tipologia selezionata.";
                ViewBag.TipoLog = logDefinition.Title;
                return View();
            }

            List<Log> logs = new List<Log>();
            foreach (var riga in righeFile)
            {
                try
                {
                    Log log = new Log(riga);
                    logs.Add(log);
                }
                catch (Exception ex)
                {
                    // Gestione dell'errore di parsing della riga del log
                    _logger.LogError($"Errore nel parsing della riga del log: {riga}. Dettagli: {ex.Message}");
                }
            }

            ViewBag.TipoLog = logDefinition.Title;
            logs.Reverse();  // Invertito per mostrare prima i log più recenti
            ViewBag.Logs = logs;
            return View();
        }

        public IActionResult ShowLevel(string livello)
        {
            if (!VerificaSessione("ADMIN"))
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di visualizzazione log per livello.");
                ViewBag.Messaggio = "Accesso non autorizzato.";
                return RedirectToAction("Index", "Home");
            }

            var livelliValidi = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ERROR",
                "WARNING",
                "INFO",
                "DEBUG"
            };

            if (string.IsNullOrWhiteSpace(livello) || !livelliValidi.Contains(livello))
            {
                ViewBag.Messaggio = "Livello di log non valido.";
                return RedirectToAction("Index");
            }

            var logs = new List<Log>();

            foreach (var definition in logDefinitions)
            {
                var righeFile = ReadLogLines(definition.Path);

                foreach (var riga in righeFile)
                {
                    try
                    {
                        var log = new Log(riga);

                        if (!string.Equals(log.TipoLog, livello, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        logs.Add(new Log
                        {
                            Timestamp = log.Timestamp,
                            TipoLog = log.TipoLog,
                            Messaggio = $"[{definition.Title}] {log.Messaggio}"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Riga log non leggibile nel dettaglio per livello.");
                    }
                }
            }

            ViewBag.TipoLog = $"Diagnostica - {livello.ToUpperInvariant()}";
            ViewBag.Logs = logs.OrderByDescending(log => log.Timestamp).ToList();
            return View("Show");
        }

        // Pagina 3 - Visualizzazione del dettaglio di un'operazione
        [HttpPost]
        public IActionResult Dettails(LogViewModel log)
        {
            if (!VerificaSessione("ADMIN"))
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di visualizzazione dettaglio log.");
                ViewBag.Messaggio = "Accesso non autorizzato.";
                return RedirectToAction("Index", "Home");
            }

           if(log == null)
            {
                ViewBag.Messaggio = "Log non trovato.";
                return RedirectToAction("Index", "Home");
            }

            return View(log);
        }

        private LogDiagnosticViewModel BuildDiagnostics()
        {
            var files = logDefinitions.Select(definition =>
            {
                var fullPath = ResolvePath(definition.Path);
                var exists = System.IO.File.Exists(fullPath);
                var size = exists ? new FileInfo(fullPath).Length : 0;
                var lines = exists ? ReadLogLines(definition.Path) : new List<string>();
                var parsedLogs = ParseLogs(lines);
                var errorCount = parsedLogs.Count(l => string.Equals(l.TipoLog, "ERROR", StringComparison.OrdinalIgnoreCase));
                var warningCount = parsedLogs.Count(l => string.Equals(l.TipoLog, "WARNING", StringComparison.OrdinalIgnoreCase));
                var status = !exists ? "missing" : errorCount > 0 ? "danger" : warningCount > 0 ? "warning" : "ok";

                return new LogFileDiagnosticViewModel
                {
                    Key = definition.Key,
                    Title = definition.Title,
                    Description = definition.Description,
                    Icon = definition.Icon,
                    AccentClass = definition.AccentClass,
                    Path = definition.Path,
                    Exists = exists,
                    SizeBytes = size,
                    Rows = lines.Count,
                    InfoCount = parsedLogs.Count(l => string.Equals(l.TipoLog, "INFO", StringComparison.OrdinalIgnoreCase)),
                    WarningCount = warningCount,
                    ErrorCount = errorCount,
                    LastEvent = parsedLogs.OrderByDescending(l => l.Timestamp).FirstOrDefault()?.Timestamp,
                    Status = status,
                    StatusLabel = status switch
                    {
                        "missing" => "File assente",
                        "danger" => "Errori presenti",
                        "warning" => "Warning presenti",
                        _ => "Regolare"
                    }
                };
            }).ToList();

            return new LogDiagnosticViewModel
            {
                Files = files,
                TotalFiles = files.Count,
                TotalBytes = files.Sum(f => f.SizeBytes),
                TotalRows = files.Sum(f => f.Rows),
                TotalWarnings = files.Sum(f => f.WarningCount),
                TotalErrors = files.Sum(f => f.ErrorCount),
                LastEvent = files.Where(f => f.LastEvent.HasValue).OrderByDescending(f => f.LastEvent).FirstOrDefault()?.LastEvent
            };
        }

        private List<Log> ParseLogs(List<string> lines)
        {
            var logs = new List<Log>();

            foreach (var line in lines)
            {
                try
                {
                    logs.Add(new Log(line));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Riga log non leggibile in diagnostica.");
                }
            }

            return logs;
        }

        private List<string> ReadLogLines(string relativePath)
        {
            var fullPath = ResolvePath(relativePath);

            if (!System.IO.File.Exists(fullPath))
            {
                return new List<string>();
            }

            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var lines = new List<string>();

            while (reader.ReadLine() is { } line)
            {
                lines.Add(line);
            }

            return lines;
        }

        private string ResolvePath(string path)
        {
            return Path.IsPathRooted(path)
                ? path
                : Path.Combine(Directory.GetCurrentDirectory(), path);
        }
    }
}
