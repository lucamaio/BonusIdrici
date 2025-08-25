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
            // Recupera il nome utente
            string username = HttpContext.Session.GetString("Username");
            
            // Recupera il ruolo dell'utente
            string ruolo = HttpContext.Session.GetString("Role");
            if(!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(ruolo)){
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult Accedi(string email, string password)
        {
            //Console.WriteLine($"Sono dentro Accedi. Email {email} | Password: {password}");
            // Verifico se le credenziali sono vuote, se lo sono torno indietro
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Message = "Inserisci username e password.";
                return RedirectToAction("Index", "Login");
            }

            // Cerco l'utente nel database
            var utente = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (utente == null)
            {
                //Console.WriteLine("Utente non trovato!");
                // Utente non trovato, ritorno alla pagina di login con un errore
                ViewBag.Message = "Username o password errati.";
                return RedirectToAction("Index", "Login");
            }

            // Memorizza i dati dell'utente nella sessione
            HttpContext.Session.SetInt32("idUser", utente.id);
            HttpContext.Session.SetString("Username", utente.Username);
            HttpContext.Session.SetString("Role", utente.getRuolo());

            // Reindirizza a una pagina protetta
            return RedirectToAction("Index", "Home");
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // cancella tutti i dati di sessione
            return RedirectToAction("Index", "Login"); // torna alla pagina di login
        }

        public IActionResult HandleError(int code)
        {
            if (code == 404)
                return View("Error404");

            if (code == 403)
                return View("Error403");

            return View("Error");
        }

    }
    
}