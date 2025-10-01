using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Models.ViewModels;
using Models;
using Data;
using System.Globalization;
using System.Collections.Generic; 
using System.IO;

namespace Controllers
{
    public class AccountController : Controller
    {
        // Inietto il logger e il DbContext
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext

        // Variabili per la gestione dell'utente
        private string? ruolo;
        private int? idUser;
        private string? username;

        // File di Log accessi 
        public static FileLog logFile = new FileLog($"wwwroot/log/utenti.log");

        public AccountController(ILogger<AccountController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;

            if (VerificaSessione())
            {
                username = HttpContext.Session.GetString("Username");
                ruolo = HttpContext.Session.GetString("Role");
                idUser = HttpContext.Session.GetInt32("idUser");
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


        // Inizio - Pagine di navigazione

        // Pagina 1: Lista utenti 
        public IActionResult Show()
        {
            // a) Verifico se esiste una sessione
            if (!VerificaSessione("ADMIN"))
            {
                ViewBag.Message = "Non sei Autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Login");
            }

            // c) Mostro la pagina
            var utenti = _context.Users.OrderBy(u => u.Username).ToList();
            ViewBag.Utenti = utenti;
            return View();
        }

        // Pagina 2: Dettagli account

        public IActionResult Dettagli()
        {
            // a) Verifico se esiste una sessione
            if (!VerificaSessione())
            {
                return RedirectToAction("Index", "Login");
            }

            // c) Cerco l'utente sul DB
            var utente = _context.Users.FirstOrDefault(s => s.id == idUser);

            if (utente == null)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Utente = utente;
            return View();
        }

        // Pagina 3: Pagina di sicurezza consente di cambiare password

        public IActionResult Sicurezza(int id)
        {
            // a) Verifico se esiste una sessione
            if (!VerificaSessione())
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.id = id;
            return View();
        }

        // Pagina 4: Pagina di impostazioni consente di modificare le impostazioni dell'account

        public IActionResult Impostazioni()
        {
            // a) Verifico se esiste una sessione
            if (!VerificaSessione())
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }

        // Pagina 5: Pagina di creazione nuovo utente

        public IActionResult Create()
        {
            // a) Verifico se esiste una sessione
            if (!VerificaSessione("ADMIN"))
            {
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

        // Pagina 6: Modifica di un utente

        public IActionResult Modifica(int id)
        {
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.id = id;
            User? utente = _context.Users.FirstOrDefault(s => s.id == id);
            if (utente == null)
            {
                ViewBag.Message = "Utente non trovato!";
                return Show();
            }
            List<Ruolo> ruoli = _context.Ruoli.ToList();
            List<Ente> enti = _context.Enti.ToList();

            List<int> selectedEntiIds = new List<int>();
            if (utente.idRuolo == 2)
            {
                selectedEntiIds = _context.UserEnti
                                        .Where(s => s.idUser == utente.id)
                                        .Select(s => s.idEnte)
                                        .ToList();
            }
        
            ViewBag.SelectedEntiIds = selectedEntiIds; // usato dalla vista per il ListBox
            ViewBag.idRuolo = utente.idRuolo;
            ViewBag.Enti = enti;
            ViewBag.Ruoli = ruoli;
            ViewBag.Utente = utente;
            return View();
        }

        // Pagina 7: Modifica la password da ADMIN ChangePassword

        public IActionResult ChangePasswordAdmin(int id)
        {
            if (!VerificaSessione("ADMIN"))
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.id = id;
            return View();
        }

    

        // Fine - Pagine di navigazione
        // Inizio - Funzioni

        // Funzione 1: Crea nuovo utente
        [HttpPost]
        public IActionResult Crea(string username, string email, string password, string? cognome, string? nome, int ruolo, List<int> enti)
        {

            // a) Verifico se esiste una sessione
            if (!VerificaSessione("ADMIN"))
            {
                ViewBag.Message = "Non sei Autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Login"); // Ritorno alla pagina di login
            }

            // c) Controllo che i dati siano stati inseriti correttamente
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(cognome) || string.IsNullOrEmpty(nome) || ruolo == 0 || enti.Count == 0)
            {
                ViewBag.Error = "Tutti i campi contrassegnati con * sono obbligatori";
                return RedirectToAction("Create"); // Ritorno alla pagina di creazione
            }

            // d) Controllo che l'username non esista già
            var userExists = _context.Users.Any(u => u.Username == username);
            if (userExists)
            {
                ViewBag.Error = "L'username esiste già. Scegliere un altro username.";
                return RedirectToAction("Create"); // Ritorno alla pagina di creazione
            }

            // e) Controllo che l'email non esista già
            var emailExists = _context.Users.Any(u => u.Email == email);
            if (emailExists)
            {
                ViewBag.Error = "L'email esiste già. Scegliere un'altra email.";
                return RedirectToAction("Create"); // Ritorno alla pagina di creazione
            }


            // f) Creo l'utente
            var newUser = new User
            {
                Username = username,
                Email = email,
                Cognome = cognome,
                Password = password,
                Nome = nome,
                idRuolo = ruolo,
                dataCreazione = DateTime.Now
            };


            _context.Users.Add(newUser);
            _context.SaveChanges();
            //Console.WriteLine("Utente creato!");

            // g) Associo l'utente agli enti selezionati
            //Console.WriteLine($"Username : {username} | Password: {password} | email: {email} | Cognome: {cognome} | Nome: {nome} | ruolo {ruolo}");
            if (ruolo == 2)
            {
                foreach (var enteId in enti)
                {
                    var ente = _context.Enti.Find(enteId);
                    //Console.WriteLine(ente.ToString());
                    if (ente != null)
                    {
                        // Qui puoi implementare la logica per associare l'utente all'ente
                        // Ad esempio, se hai una tabella di associazione UserEnte, puoi aggiungere una nuova voce lì
                        var userEnte = new UserEnte
                        {
                            idUser = newUser.id,
                            idEnte = enteId
                        };
                        _context.UserEnti.Add(userEnte);
                    }
                    _context.SaveChanges();
                    //Console.WriteLine("Enti Collegati!");
                }
            }

            ViewBag.Message = "Utente creato con successo";
            return RedirectToAction("Show");
        }


        // Funzione 2: Reset/Modifica password

        // ....

        // Funzione 3: Modifica dati utente
        [HttpPost]
        public IActionResult Update(int id, string username, string cognome, string nome, string email, string password)
        {
            var UtenteEsistente = _context.Users.FirstOrDefault(t => t.id == id);

            if (UtenteEsistente == null)
            {
                return RedirectToAction("Index", "Home"); // oppure restituisci una view con errore
            }

            // Aggiorna le proprietà
            UtenteEsistente.Cognome = FunzioniTrasversali.rimuoviVirgolette(cognome);
            UtenteEsistente.Nome = FunzioniTrasversali.rimuoviVirgolette(nome);

            if (UtenteEsistente.Username != username)
            {
                UtenteEsistente.Username = FunzioniTrasversali.rimuoviVirgolette(username);
            }

            if (UtenteEsistente.Email != email)
            {
                UtenteEsistente.Email = FunzioniTrasversali.rimuoviVirgolette(email);
            }

            if (UtenteEsistente.Password != password)
            {
                UtenteEsistente.Password = FunzioniTrasversali.rimuoviVirgolette(password);
            }

            UtenteEsistente.dataAggiornamento = DateTime.Now;

            _context.SaveChanges();

            return RedirectToAction("Show");
        }

        // Funzione 4: Funzione che reimposta la password da ADMIN

        public IActionResult updatePassword(int id, string password)
        {
            if (!VerificaSessione("ADMIN"))
            {
                ViewBag.Message = "Utente non autorizzato ad effetuare questa operazione";
                return RedirectToAction("Index", "Home");
            }

            // Mi ricavo l'utente dal suo id

            var utente = _context.Users.FirstOrDefault(s => s.id == id);

            if (utente == null)
            {
                ViewBag.Message = "Utente non trovato!";
                return RedirectToAction("Show");
            }

            // Aggiorno la password e la data di aggiornamento

            utente.Password = password;
            utente.dataAggiornamento = DateTime.Now;

            _context.Users.Update(utente);
            _context.SaveChanges();

            ViewBag.Message = "Password Cambiata con successo!";
            return RedirectToAction("Modifica", "Account", new { id = id });

        }

        // Funzione 5: Funzione che reimposta la password di un user

        public IActionResult ChangePassword(int id, string password, string newPassword, string confirmPassword)
        {
            // 1. Ricavo l'utente a partire dal id
            var utente = _context.Users.FirstOrDefault(s => s.id == id);

            if (utente == null)
            {
                ViewBag.Message = "Utente non trovato!";
                return RedirectToAction("Index", "Home");
            }

            // 2. Verifico se la password attuale è corretta

            if (utente.Password != password)
            {
                ViewBag.Message = "La password attuale non è corretta";
                return RedirectToAction("Dettagli", "Account", new { id = id });
            }


            // 3. Verifico che le password non sono diverse

            if (newPassword != confirmPassword)
            {
                ViewBag.Message = "Le password non coincidono!";
                return RedirectToAction("Dettagli", "Account", new { id = id });
            }

            // 4. Aggiorno la password

            utente.Password = newPassword;
            utente.dataAggiornamento = DateTime.Now;

            _context.Users.Update(utente);
            _context.SaveChanges();

            ViewBag.Message = "Password Cambiata con successo!";
            return RedirectToAction("Dettagli", "Account", new { id = id });
        }

    }
}