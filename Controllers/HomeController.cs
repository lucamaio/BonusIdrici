using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BonusIdrici2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Pagine di navigazione
        public IActionResult Index()
        {
            int? idUtente = HttpContext.Session.GetInt32("idUser");

            // Recupera il nome utente
            string username = HttpContext.Session.GetString("Username");

            // Recupera il ruolo dell'utente
            string ruolo = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username))
            {
                // Se non trovi il nome utente, la sessione è scaduta o non esiste.
                // Reindirizza l'utente alla pagina di login.
                return RedirectToAction("Index", "Login");
            }

            // Passa i dati alla View tramite ViewBag o un Model
            ViewBag.IdUtente = idUtente;
            ViewBag.Username = username;
            ViewBag.Ruolo = ruolo;

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult HandleError(int code)
        {
            if (code == 404)
                return View("Error404");

            if (code == 403)
                return View("Error403");

            return View("Error");
        }

        // Aggiungere una pagina info
    }
    
}