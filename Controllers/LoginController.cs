using Microsoft.AspNetCore.Mvc;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;

namespace BonusIdrici2.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly ApplicationDbContext _context;

        public LoginController(ILogger<LoginController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Pagina di login
        public IActionResult Index()
        {
            // Controlla se l'utente è già loggato
            string username = HttpContext.Session.GetString("Username");
            string ruolo = HttpContext.Session.GetString("Role");

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(ruolo))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Accedi(string email, string password)
        {
            // Controllo credenziali vuote
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["Message"] = "Inserisci username e password.";
                return RedirectToAction("Index");
            }

            // Cerca l'utente nel database
            var utente = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (utente == null)
            {
                TempData["Message"] = "Username o password errati.";
                return RedirectToAction("Index");
            }

            // Salva i dati dell'utente in sessione
            HttpContext.Session.SetInt32("idUser", utente.id);
            HttpContext.Session.SetString("Username", utente.Username);
            HttpContext.Session.SetString("Role", utente.getRuolo());

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Pulisce la sessione
            TempData["Message"] = "Logout effettuato con successo!";
            return RedirectToAction("Index");
        }
    }
}
