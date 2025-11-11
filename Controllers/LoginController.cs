using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;  // Neccessario per la gestione delle identità
using Models; 
using Data;

namespace Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly ApplicationDbContext _context;


         // Variabili per la gestione dell'utente
        private string? ruolo;
        private int idUser;
        private string? username;


        public LoginController(ILogger<LoginController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;

             if (VerificaSessione())
            {
                username = HttpContext.Session.GetString("Username");
                ruolo = HttpContext.Session.GetString("Role");
                idUser = (int)HttpContext.Session.GetInt32("idUser");
            }
        }

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

        // Inizio Sezione - Pagine di Navigazione

        // Pagina 1: Consente di effetuare il login al sistema
        public IActionResult Index()
        {
            // 1) Verifico se l'utente è già loggato
            if (VerificaSessione())
            {
                return RedirectToAction("Index", "Home");
            }
            // 2) Verifico se ci sono utenti nel sistema
            var utenti = _context.Users.ToList();
            if (utenti.Count == 0)
            {
                TempData["Message"] = "Nessun utente trovato nel sistema. Crea un utente ADMIN per accedere.";
                return RedirectToAction("FirstRegister", "Account");
            }

            return View();
        }

        // Fine Sezione - Pagine di Navigazione

        // Inizio Sezione - Azioni
        // Azione 1: Consente di effetuare il login al sistema
        [HttpPost]
        public IActionResult Accedi(string username, string password)
        {
            // Ottieni l'indirizzo IP dell'utente (Da sistemare in futuro)
            // var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            // Console.WriteLine("IP utente: " + ipAddress);
            // logFile.LogInfo($"Tentativo di accesso da IP: {ipAddress} ");

            // 1) Controllo se le credenziali sono vuote
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["Message"] = "Inserisci username e password.";
                return RedirectToAction("Index");
            }

            // 2) Verifico le credenziali dell'utente fornite

            var utente = _context.Users.FirstOrDefault(u => u.Email == username || u.Username == username);
            if (utente == null)
            {
                TempData["Message"] = "Username o password errati.";
                return RedirectToAction("Index");
            }

            // 3) Verifico la password
            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(utente, utente.Password, password);
           
            if (result == PasswordVerificationResult.Success)
            {
                // Password corretta, procedo con il login

                // 4) Salva i dati dell'utente in sessione
                HttpContext.Session.SetInt32("idUser", utente.id);
                HttpContext.Session.SetString("Username", utente.Username);
                HttpContext.Session.SetString("Role", utente.getRuolo());
                HttpContext.Session.SetString("Tema", "Dark");

                // 5) Salvo un messaggio di log per l'accesso effettuato
                AccountController.logFile.LogInfo($"Utente logato: {utente.Username} ");
                return RedirectToAction("Index", "Home");
            }
           
            TempData["Message"] = "Username o password errati.";
            return RedirectToAction("Index");
        }
        // Azione 2: Consente di effetuare il logout dal sistema
        public IActionResult Logout()
        {
            AccountController.logFile.LogInfo($"Utente disconesso: {username} ");
            HttpContext.Session.Clear(); // Pulisce la sessione
            TempData["Message"] = "Logout effettuato con successo!";
            return RedirectToAction("Index");
        }
        // Fine Sezione - Azioni
    }
}
