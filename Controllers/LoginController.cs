using Microsoft.AspNetCore.Mvc;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;
using System.IO;

namespace BonusIdrici2.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext
        public LoginController(ILogger<LoginController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Pagine di navigazione

          public IActionResult Index()
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