using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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


        // Pagina di login
        public IActionResult Index()
        {
            if(VerificaSessione()){
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Accedi(string email, string password)
        {
            // Ottieni l'indirizzo IP dell'utente (Da sistemare in futuro)
            // var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            // Console.WriteLine("IP utente: " + ipAddress);
            // logFile.LogInfo($"Tentativo di accesso da IP: {ipAddress} ");

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
            // Salvo il messagio di un nuovo accesso
            AccountController.logFile.LogInfo($"Utente logato: {utente.Username} ");

            // Salva i dati dell'utente in sessione
            HttpContext.Session.SetInt32("idUser", utente.id);
            HttpContext.Session.SetString("Username", utente.Username);
            HttpContext.Session.SetString("Role", utente.getRuolo());

            return RedirectToAction("Index", "Home");
        }
    
        public IActionResult Logout()
        {
            AccountController.logFile.LogInfo($"Utente disconesso: {username} ");
            HttpContext.Session.Clear(); // Pulisce la sessione
            TempData["Message"] = "Logout effettuato con successo!";
            return RedirectToAction("Index");
        }
    }
}
