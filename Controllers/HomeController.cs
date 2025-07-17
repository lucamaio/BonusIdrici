using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;
using System.IO;
using Dichiarante;
using Atto;
using leggiCSV;
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
            // if (User.Identity.IsAuthenticated)
            // {
            //     ViewData["Message"] = $"Benvenuto, {User.Identity.Name}! Hai effettuato l'accesso.";
            //     // Puoi anche recuperare dati dalla sessione
            //     string nomeUtenteInSessione = HttpContext.Session.GetString("NomeUtente");
            //     ViewData["SessionMessage"] = $"Nome in sessione: {nomeUtenteInSessione}";
            // }
            // else
            // {
            //     ViewData["Message"] = "Non hai effettuato l'accesso.";
            // }
            return View();
        }

          public IActionResult Login()
        {
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


        // Esempio nel tuo HomeController.cs o LoginController.cs
        // [HttpPost]
        // public async Task<IActionResult> Logout()
        // {
        //     await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        //     HttpContext.Session.Clear(); // Opzionale: pulisce anche i dati specifici della sessione

        //     return RedirectToAction("Index", "Home"); // Reindirizza alla home page o pagina di login
        // }

    }
    
}