using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using Models; 
using Data;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using BonusIdrici2.Services;

namespace Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext
        private readonly SectionActivityService _sectionActivityService;
        private readonly AppCacheService _cache;

        // Variuabili di sessione

        private string? ruolo;
        private int? idUser;
        private string? username;

        private string tema;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, SectionActivityService sectionActivityService, AppCacheService cache)
        {
            _logger = logger;
            _context = context;
            _sectionActivityService = sectionActivityService;
            _cache = cache;

            if (VerificaSessione())
            {
                username = HttpContext.Session.GetString("Username");
                ruolo = HttpContext.Session.GetString("Role");
                idUser = HttpContext.Session.GetInt32("idUser");
                tema = HttpContext.Session.GetString("Tema") ?? "Default";
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
            tema = HttpContext.Session.GetString("Tema") ?? "Default";

            if (!VerificaSessione())
            {
                username = null;
                ruolo = null;
                idUser = 0;
                tema = "Default";
            }

            // Così le variabili sono disponibili in tutte le viste
            ViewBag.idUser = idUser;
            ViewBag.Username = username;
            ViewBag.Ruolo = ruolo;
            ViewBag.Tema = tema;
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


        // Pagine di navigazione
        public IActionResult Index()
        {
            if (!VerificaSessione())
            {
                // Se non trovi il nome utente, la sessione è scaduta o non esiste.
                // Reindirizza l'utente alla pagina di login.
                return RedirectToAction("Index", "Login");
            }

            // Passa i dati alla View tramite ViewBag o un Model
            ViewBag.IdUtente = idUser;
            ViewBag.Username = username;
            ViewBag.Ruolo = ruolo;
            ViewBag.Tema = tema;

            return View();
        }

        // Pagina 2: Error
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Pagina 3: Privacy Policy
        public IActionResult Privacy()
        {
            return View();
        }

        // Pagina 4: Error 403 e error 404

        public IActionResult HandleError(int code)
        {
            if (code == 404)
                return View("Error404");

            if (code == 403)
                return View("Error403");

            return View("Error");
        }

        // Pagina 5: Pagina info

        public IActionResult Info()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            if (!VerificaSessione())
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }

        public IActionResult Attivita()
        {
            if (!VerificaSessione("ADMIN"))
            {
                return RedirectToAction("Index", "Home");
            }

            return View(_sectionActivityService.GetAdminActivityDashboard());
        }

        public IActionResult ClearCache()
        {
            if (!VerificaSessione("ADMIN") || idUser != 1)
            {
                AccountController.logFile.LogWarning("Tentativo non autorizzato di svuotamento cache applicativa.");
                return Forbid();
            }

            _cache.ClearAll();
            AccountController.logFile.LogInfo("Cache applicativa svuotata manualmente dall'amministratore.");
            ViewBag.Message = "Cache applicativa svuotata correttamente.";
            return RedirectToAction("Index", "Home");
        }
    }
    
}
