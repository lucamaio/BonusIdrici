using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using Models; 
using Data;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext

        // Variuabili di sessione

        private string? ruolo;
        private int? idUser;
        private string? username;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
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

            return View();
        }

        // Pagina 2: Error
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Pagina 3: Privacy Policy
        // public IActionResult Privacy()
        // {
        //     return View();
        // }

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
    }
    
}