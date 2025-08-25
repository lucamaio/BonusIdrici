using Microsoft.AspNetCore.Mvc;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;
using System.IO;

namespace BonusIdrici2.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext

        public AccountController(ILogger<AccountController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Show(){
             // a) Verifico se esiste una sessione
            if(!VerificaSessione()){
               return RedirectToAction("Index", "Login");
            }

            // b) Verifico se l'utente è Admin
            var role=HttpContext.Session.GetString("Role");
            if(role !="ADMIN"){
                ViewBag.Message = "Non sei Autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }
            
            // c) Mostro la pagina
            var utenti = _context.Users.OrderBy(u => u.Username).ToList();
            ViewBag.Utenti = utenti;
            return View();
        }

        public IActionResult Dettagli(){
            // a) Verifico se esiste una sessione
            if(!VerificaSessione()){
               return RedirectToAction("Index", "Login");
            }

            // b) Mi salvo User
            var idUser=HttpContext.Session.GetInt32("idUser");

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

        public IActionResult Sicurezza(){
             // a) Verifico se esiste una sessione
            if(!VerificaSessione()){
               return RedirectToAction("Index", "Login");
            }

            return View();
        }

        public IActionResult Impostazioni(){
             // a) Verifico se esiste una sessione
            if(!VerificaSessione()){
               return RedirectToAction("Index", "Login");
            }

            return View();
        }

        public IActionResult Create()
        {
            // a) Verifico se esiste una sessione
            if(!VerificaSessione()){
               return RedirectToAction("Index", "Login");
            }

            // b) Verifico se l'utente è Admin
            var role=HttpContext.Session.GetString("Role");
            if(role !="ADMIN"){
                ViewBag.Message = "Non sei Autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // c) Mostro la pagina
            var ruoli = _context.Ruoli.OrderBy(r => r.id).Select(r => new { r.id, r.nome }).ToList();
            ViewBag.Ruoli = ruoli;

            var enti = _context.Enti.OrderBy(e => e.id).Select(e => new { e.id, e.nome }).ToList();
            ViewBag.Enti = enti;
            return View();
        }

        // Funzione per creare un nuovo utente

        [HttpPost]
        public IActionResult Crea(string username, string email, string? cognome, string? nome, int ruolo, List<int> enti){
            // a) Verifico se esiste una sessione
            if(!VerificaSessione()){
               return RedirectToAction("Index", "Login"); // Ritorno alla pagina di login
            }

            // b) Verifico se l'utente è Admin
            var role=HttpContext.Session.GetString("Role");
            if(role !="ADMIN"){
                ViewBag.Message = "Non sei Autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");  // Ritorno alla home
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


        // Da implementare reset password e modifica account

        // ....

        // Funzione che controlla se esiste una funzione e se il ruolo e uguale a quello richiesto per accedere alla pagina desiderata
        public bool VerificaSessione(string ruoloRichiesto = null)
        {
            string username = HttpContext.Session.GetString("Username");
            string ruolo = HttpContext.Session.GetString("Role");

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

    }
}