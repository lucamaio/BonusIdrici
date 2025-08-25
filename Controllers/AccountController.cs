using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BonusIdrici2.Models.ViewModels;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;
using System.IO;

namespace BonusIdrici2.Controllers
{
    public class AccountController : Controller
    {
        // Inietto il logger e il DbContext
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext

        // Variabili per la gestione dell'utente
        private string? ruolo;
        private int idUser;
        private string? username;

        public AccountController(ILogger<AccountController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;

            if (VerificaSessione())
            {
                username = HttpContext.Session.GetString("Username");
                ruolo = HttpContext.Session.GetString("Role");
                idUser = (int) HttpContext.Session.GetInt32("idUser");
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

        public bool VerificaSessione(string ruoloRichiesto = null)
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

        // Pagina 1: Lista utenti 
        public IActionResult Show(){
             // a) Verifico se esiste una sessione
            if(!VerificaSessione("ADMIN")){
                ViewBag.Message = "Non sei Autorizzato ad accedere a questa pagina";
               return RedirectToAction("Index", "Login");
            }

            // c) Mostro la pagina
            var utenti = _context.Users.OrderBy(u => u.Username).ToList();
            ViewBag.Utenti = utenti;
            return View();
        }

        // Pagina 2: Dettagli account

        public IActionResult Dettagli(){
            // a) Verifico se esiste una sessione
            if(!VerificaSessione()){
               return RedirectToAction("Index", "Login");
            }

            // c) Cerco l'utente sul DB
            var utente = _context.Users.Where(s=> s.id==idUser).ToList();

            if(utente == null){
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Username = utente[0].Username;
            ViewBag.Email = utente[0].Email;
            ViewBag.Cognome = utente[0].Cognome;
            ViewBag.Nome = utente[0].Nome;
            ViewBag.Role = utente[0].getRuolo();
            return View();
        }

        // Pagina 3: Pagina di sicurezza consente di cambiare password

        public IActionResult Sicurezza(){
             // a) Verifico se esiste una sessione
            if(!VerificaSessione()){
               return RedirectToAction("Index", "Login");
            }

            return View();
        }

        // Pagina 4: Pagina di impostazioni consente di modificare le impostazioni dell'account

        public IActionResult Impostazioni(){
             // a) Verifico se esiste una sessione
            if(!VerificaSessione()){
               return RedirectToAction("Index", "Login");
            }

            return View();
        }

        // Pagina 5: Pagina di creazione nuovo utente

        public IActionResult Create()
        {
            // a) Verifico se esiste una sessione
            if(!VerificaSessione("ADMIN")){
                ViewBag.Message = "Non sei Autorizzato ad accedere a questa pagina";
               return RedirectToAction("Index", "Login");
            }

            // c) Mostro la pagina
            var ruoli = _context.Ruoli.OrderBy(r => r.id).Select(r => new { r.id, r.nome }).ToList();
            ViewBag.Ruoli = ruoli;

            var enti = _context.Enti.OrderBy(e => e.id).Select(e => new { e.id, e.nome }).ToList();
            ViewBag.Enti = enti;
            return View();
        }
        // Fine - Pagine di navigazione
        // Inizio - Funzioni

        // Funzione 1: Crea nuovo utente
        [HttpPost]
        public IActionResult Crea(string username, string email, string? cognome, string? nome, int ruolo, List<int> enti){
            // a) Verifico se esiste una sessione
            if(!VerificaSessione("ADMIN")){
                ViewBag.Message = "Non sei Autorizzato ad accedere a questa pagina";
               return RedirectToAction("Index", "Login"); // Ritorno alla pagina di login
            }

            // c) Controllo che i dati siano stati inseriti correttamente
            if(string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(cognome) || string.IsNullOrEmpty(nome) || ruolo==0 || enti.Count==0){
                ViewBag.Error = "Tutti i campi contrassegnati con * sono obbligatori";
                return Create(); // Ritorno alla pagina di creazione
            }

            // d) Controllo che l'username non esista già
            var userExists = _context.Users.Any(u => u.Username == username);
            if(userExists){
                ViewBag.Error = "L'username esiste già. Scegliere un altro username.";
                return Create(); // Ritorno alla pagina di creazione
            }

            // e) Controllo che l'email non esista già
            var emailExists = _context.Users.Any(u => u.Email == email);
            if(emailExists){
                ViewBag.Error = "L'email esiste già. Scegliere un'altra email.";
                return Create(); // Ritorno alla pagina di creazione
            }

            // f) Creo l'utente
            var newUser = new User{
                Username = username,
                Email = email,
                Cognome = cognome,
                Nome = nome,
                idRuolo = ruolo,
                dataCreazione = DateTime.Now
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            // g) Associo l'utente agli enti selezionati
            foreach(var enteId in enti){
                var ente = _context.Enti.Find(enteId);
                if(ente != null){
                    // Qui puoi implementare la logica per associare l'utente all'ente
                    // Ad esempio, se hai una tabella di associazione UserEnte, puoi aggiungere una nuova voce lì
                    var userEnte = new UserEnte{
                        idUser = newUser.id,
                        idEnte = enteId
                    };
                    _context.UserEnti.Add(userEnte);
                }
                 _context.SaveChanges();
            }

            ViewBag.Message = "Utente creato con successo";
            return Show();
        }

        public IActionResult HandleError(int code)
        {
            if (code == 404)
                return View("Error404");

            if (code == 403)
                return View("Error403");

            return View("Error");
        }


        // Funzione 2: Reset/Modifica password

        // ....

        // Funzione 3: Modifica dati utente

        // ....

    }
}