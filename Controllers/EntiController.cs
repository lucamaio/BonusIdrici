using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using Models; 
using Data;
using System.IO;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Models.ViewModels; 

namespace Controllers
{
    public class EntiController : Controller
    {
        private readonly ILogger<EntiController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext

         private string? ruolo;
        private int? idUser;
        private string? username;

        public EntiController(ILogger<EntiController> logger, ApplicationDbContext context)
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
                idUser = 0;
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

        // Inizio - Pagine di navigazione

        // Pagina home che consente la selezione del ente
        public IActionResult Index()
        {
            if (!VerificaSessione("ADMIN"))
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di gestione enti.");
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.idUser = HttpContext.Session.GetString("idUser");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Ruolo = HttpContext.Session.GetString("Role");

            var dati = _context.Enti.ToList();
            var viewModelList = dati.Select(x => new EntiViewModel
            {
                id = x.id,
                nome = x.nome,
                istat = x.istat,
                partitaIva = x.partitaIva,
                CodiceFiscale = x.CodiceFiscale,
                Cap = x.Cap,
                Provincia = x.Provincia,
                Regione = x.Regione,
                Serie = x.Serie,
                Piranha = x.Piranha,
                Selene = x.Selene,
                DataCreazione = x.DataCreazione,
                DataAggiornamento = x.DataAggiornamento
            }).ToList();

            return View("Index", viewModelList);
        }

        // Pagina per la creazione di un nuovo Ente
        public IActionResult Create()
        {
            if (!VerificaSessione("ADMIN"))
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di creazione nuovo ente.");
                ViewBag.Message = "Utente non autorizzato ad creare un nuovo ente";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.idUser = HttpContext.Session.GetString("idUser");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Ruolo = HttpContext.Session.GetString("Role");

            return View();
        }

        // Pagina 3: Pagina per la modificha dei dati di un ente

        public IActionResult Modifica(int id)
        {
            if (!VerificaSessione("ADMIN"))
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di modifica dati ente.");
                ViewBag.Message = "Utente non autorizzato alla modifica dei dati del ente";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.idUser = HttpContext.Session.GetString("idUser");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Ruolo = HttpContext.Session.GetString("Role");

            var ente = _context.Enti.FirstOrDefault(s => s.id == id);
            if (ente == null)
            {
                ViewBag.Message = "Ente non trovato";
                return RedirectToAction("Index", "Home");
            }
            var usernameCreatore = _context.Users.FirstOrDefault(u => u.id == ente.IdUser)?.Username ?? "Sconosciuto";
            ViewBag.UsernameCreatore = usernameCreatore;
            ViewBag.id = id;
            ViewBag.Ente = ente;
            return View();
        }

        // Fine - Pagine di Navigazione

        // Inizio - Funzioni da eseguire a seconda della operazione

        // Funzione che viene eseguita dopo aver compilato il form per la creazione di un ente

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpPost]
        public IActionResult Crea(String nome, string istat, string partitaIva, string cap, string? CodiceFiscale, string? provincia, string? regione,int serie, bool Piranha, bool Selene)
        {
            if (!VerificaSessione("ADMIN"))
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad accedere a questa pagina. Ha invocato la funzione Crea.");
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.idUser = HttpContext.Session.GetString("idUser");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Ruolo = HttpContext.Session.GetString("Role");

            var ente = new Ente
            {
                nome = nome.Trim().ToUpper(),
                istat = istat.Trim().ToUpper(),
                partitaIva = partitaIva.Trim().ToUpper(),
                Cap = cap.Trim(),
                Provincia = provincia?.Trim().ToUpper(),
                Regione = regione?.Trim().ToUpper(),
                CodiceFiscale = CodiceFiscale?.Trim().ToUpper(),
                Serie = serie,
                Piranha = Piranha,
                Selene = Selene,
                DataCreazione = DateTime.Now,
                IdUser = idUser ?? 0,
            };
            
            ViewBag.Message = "Ente creato con successo";
            return Index();
        }
       
        
        // Funzione che viene eseguita per aggiornare i dati del ente con queli inseriti nel form

        [HttpPost]
        public IActionResult Update(int id, string nome, string istat, string partitaIva, string cap, string? CodiceFiscale, string? provincia, string? regione, int serie, bool? Piranha, bool? Selene)
        {
            if (!VerificaSessione("ADMIN"))
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad accedere a questa pagina. Ha invocato la funzione Update.");
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            var enteEsistente = _context.Enti.FirstOrDefault(s => s.id == id);

            if (enteEsistente == null)
            {
                return RedirectToAction("Index", "Home");
            }

            enteEsistente.nome = nome.Trim().ToUpper();
            enteEsistente.istat = istat.Trim().ToUpper();
            enteEsistente.partitaIva = partitaIva.Trim().ToUpper();
            enteEsistente.Cap = cap.Trim();
            enteEsistente.Provincia = provincia?.Trim().ToUpper();
            enteEsistente.Regione = regione?.Trim().ToUpper();
            enteEsistente.CodiceFiscale = CodiceFiscale?.Trim().ToUpper();
            enteEsistente.DataAggiornamento = DateTime.Now;

            if (serie != enteEsistente.Serie)
            {
                enteEsistente.Serie = serie;
            }

            // Se il checkbox non è selezionato, il valore sarà null, quindi impostiamo a false
            enteEsistente.Piranha = Piranha ?? false;
            enteEsistente.Selene = Selene ?? false;

            _context.SaveChanges();
            ViewBag.Message = "Dati ente aggiornati con successo";
            return Index();
        }

        // Fine - Funzioni da eseguire a seconda della operazione

    }
}