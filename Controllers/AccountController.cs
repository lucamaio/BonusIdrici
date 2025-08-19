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