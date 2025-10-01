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

        private List<string> tipologieLog = new List<string> { "Accessi", "Esportazioni", "CaricamentoUtenze", "CaricamentoAnagrafiche" };

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
                return RedirectToAction("Index", "Home");
            }
            ViewBag.TipologieLog = tipologieLog;
            return View();
        }

        // Pagina 2 - Visualizzazione del log delle operazioni

        public IActionResult Show(string tipoLog)
        {
            if (!VerificaSessione("ADMIN"))
            {
                ViewBag.Messaggio = "Accesso non autorizzato.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(tipoLog) || (tipoLog != "Accessi" && tipoLog != "Esportazioni" && tipoLog != "CaricamentoUtenze" && tipoLog != "CaricamentoAnagrafiche"))
            {
                ViewBag.Messaggio = "Tipologia di log non valida.";
                return RedirectToAction("Index");
            }

            string? percorsoFile = string.Empty;
            List<string>? righeFile = new List<string>();

            switch (tipoLog)
            {
                case "Accessi":
                    percorsoFile = ($"wwwroot/log/utenti.log");
                    righeFile = FileReader.ReadLines(percorsoFile);
                    break;
                case "Esportazioni":
                    percorsoFile = ($"wwwroot/log/Elaborazione_INPS.log");
                    righeFile = FileReader.ReadLines(percorsoFile);
                    break;
                case "CaricamentoUtenze":
                    percorsoFile = ($"wwwroot/log/Lettura_UtenzeIdriche.log");
                    righeFile = FileReader.ReadLines(percorsoFile);
                    break;
                case "CaricamentoAnagrafiche":
                    percorsoFile = ($"wwwroot/log/Elaborazione_Anagrafe.log");
                    righeFile = FileReader.ReadLines(percorsoFile);
                    break;
                default:
                    return RedirectToAction("Index", "Home");
            }

            if (righeFile == null || righeFile.Count == 0)
            {
                ViewBag.Messaggio = "Nessun log disponibile per la tipologia selezionata.";
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

            ViewBag.TipoLog = tipoLog;
            logs.Reverse();  // Invertito per mostrare prima i log più recenti
            ViewBag.Logs = logs;
            return View();
        }

        // Pagina 3 - Visualizzazione del dettaglio di un'operazione
        [HttpPost]
        public IActionResult Dettails(LogViewModel log)
        {
            if (!VerificaSessione("ADMIN"))
            {
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
    }
}