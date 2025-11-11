using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;  // Neccessario per la gestione delle identità
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
        private string tema;

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
                tema = HttpContext.Session.GetString("Tema") ?? "Default";
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
            tema = HttpContext.Session.GetString("Tema") ?? "Default";

            if (!VerificaSessione())
            {
                username = null;
                ruolo = null;
                idUser = 0;
                tema = "Default";
            }

            // Così le variabili sono disponibili in tutte le viste
            ViewBag.idUser = idUser;
            ViewBag.Username = username;
            ViewBag.Ruolo = ruolo;
            ViewBag.Tema = tema;
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

        // Pagina 0 : Consente di effetuare la prima registrazione di un utente ADMIN se non esistono utenti nel sistema

        public IActionResult FirstRegister()
        {
            // 1) Verifico se l'utente è già loggato
            if (VerificaSessione())
            {
                logFile.LogWarning($"Utente {username} ha tentato di accedere alla pagina di FirstRegister pur essendo già loggato.");
                ViewBag.Message = "Sei già loggato nel sistema.";
                return RedirectToAction("Index", "Home");
            }

            // 2) Verifico se esistono utenti nel sistema
            var utenti = _context.Users.ToList();
            if (utenti.Count > 0)
            {
                logFile.LogWarning("Tentativo di accesso alla pagina di FirstRegister nonostante esistano già utenti nel sistema.");
                TempData["Message"] = "Esistono già utenti nel sistema. Effettua il login.";
                return RedirectToAction("Index", "Login");
            }
            // 3) Reindirizzo alla pagina di registrazione
            return View();
        }


        // Pagina 1: Lista utenti 
        public IActionResult Show()
        {
            // a) Verifico se esiste una sessione
            if (!VerificaSessione("ADMIN"))
            {
                logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di visualizzazione utenti.");
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
                logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di dettagli account.");
                ViewBag.Message = "Non sei Autorizzato ad accedere a questa pagina";
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
                logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di sicurezza account.");
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
                logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di impostazioni account.");
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
                logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di creazione nuovo utente.");
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
                logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di modifica utente.");
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

        // Funzione 0: Crea il primo utente ADMIN se non esistono utenti nel sistema
        [HttpPost]
        public IActionResult CreateFristUser(string email, string password, string cognome, string nome)
        {
            if (VerificaSessione())
            {
                logFile.LogWarning($"Utente {username} ha tentato di accedere alla funzione CreateFirstUser pur essendo già loggato.");
                ViewBag.Message = "Sei già loggato nel sistema.";
                return RedirectToAction("Index", "Home");
            }
            
            // 1) Verifico se esistono utenti nel sistema
            var utenti = _context.Users.ToList();
            if (utenti.Count > 0)
            {
                TempData["Message"] = "Esistono già utenti nel sistema. Effettua il login.";
                return RedirectToAction("Index", "Login");
            }

            // 2) Creo il primo utente ADMIN
            var hasher = new PasswordHasher<User>();

            var newUser = new User
            {
                Username = "Admin",
                Email = email,
                Cognome = cognome,
                Password = "", // Verrà sostituita dopo l'hashing"
                Nome = nome,
                idRuolo = 1, // Ruolo ADMIN
                dataCreazione = DateTime.Now
            };

            string hashedPassword = hasher.HashPassword(newUser, password);
            newUser.Password = hashedPassword;

            _context.Users.Add(newUser);
            _context.SaveChanges();

            logFile.LogInfo($"Primo utente ADMIN creato: {username}");

            TempData["Message"] = "Utente ADMIN creato con successo. Effettua il login.";
            return RedirectToAction("Index", "Login");
        }

        // Funzione 1: Crea nuovo utente
        [HttpPost]
        public IActionResult Crea(string username, string email, string password, string? cognome, string? nome, int ruolo, List<int> enti)
        {

            // 1) Verifico se esiste una sessione ed il ruolo è ADMIN
            if (!VerificaSessione("ADMIN"))
            {
                logFile.LogWarning($"Tentativo di accesso non autorizzato alla creazione di un utente da parte di {username}");
                ViewBag.Message = "Non sei Autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Login"); // Ritorno alla pagina di login
            }

            // 2) Verifico che ho i dati necessari per creare l'utente
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(cognome) || string.IsNullOrEmpty(nome) || ruolo == 0 || enti.Count == 0)
            {
                ViewBag.Error = "Tutti i campi contrassegnati con * sono obbligatori";
                return RedirectToAction("Create"); // Ritorno alla pagina di creazione
            }

            // 3) Controllo che l'utente non esista già con lo stesso username o email

            var userExists = _context.Users.Any(u => u.Username == username || u.Email == email);
            if (userExists)
            {
                ViewBag.Error = "L'utente esiste già. Scegliere un altro username.";
                return RedirectToAction("Create"); // Ritorno alla pagina di creazione
            }

            // 4) Protezzione password se rispetta i criteri minimi (di lunghezza minima 8 caratteri, almeno una lettera maiuscola, una minuscola, un numero e un carattere speciale)
            // if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
            // {
            //     ViewBag.Error = "La password non soddisfa i criteri di sicurezza. Deve contenere almeno 8 caratteri, una lettera maiuscola, una minuscola, un numero e un carattere speciale.";
            //     return RedirectToAction("Create"); // Ritorno alla pagina di creazione
            // }
           
            // 5) Crittografo la password con PasswordHasher e creo l'utente

            var hasher = new PasswordHasher<User>();

            var newUser = new User
            {
                Username = username,
                Email = email,
                Cognome = cognome,
                Password = "", // Verrà sostituita dopo l'hashing"
                Nome = nome,
                idRuolo = ruolo,
                dataCreazione = DateTime.Now
            };

           string hashedPassword = hasher.HashPassword(newUser, password);
           newUser.Password = hashedPassword;

            _context.Users.Add(newUser);
            _context.SaveChanges();

            // 6) Se il ruolo è OPERATORE collego l'utente agli enti selezionati
            if (ruolo == 2)
            {
                foreach (var enteId in enti)
                {
                    var ente = _context.Enti.Find(enteId);
                    if (ente != null)
                    {
                        var userEnte = new UserEnte
                        {
                            idUser = newUser.id,
                            idEnte = enteId
                        };
                        _context.UserEnti.Add(userEnte);
                    }
                    _context.SaveChanges();
                }
            }
            
            // 7) Registro l'evento e ritorno alla lista utenti
            logFile.LogInfo($"Nuovo utente creato: {username} con ruolo {ruolo} da parte di {HttpContext.Session.GetString("Username")}");
            ViewBag.Message = "Utente creato con successo";
            return RedirectToAction("Show");
        }

        // Funzione 2: Modifica dati utente
        [HttpPost]
        public IActionResult Update(int id, string username, string cognome, string nome, string email)
        {
            var UtenteEsistente = _context.Users.FirstOrDefault(t => t.id == id);

            if (UtenteEsistente == null)
            {
                return RedirectToAction("Index", "Home"); // oppure restituisci una view con errore
            }
            bool datiModificati = false;

            // Aggiorna le proprietà se sono diverse

            if (UtenteEsistente.Cognome != FunzioniTrasversali.rimuoviVirgolette(cognome))
            {
                UtenteEsistente.Cognome = FunzioniTrasversali.rimuoviVirgolette(cognome);
                datiModificati = true;
            }

            if (UtenteEsistente.Nome != FunzioniTrasversali.rimuoviVirgolette(nome))
            {
                datiModificati = true;
            }

            if (UtenteEsistente.Username != FunzioniTrasversali.rimuoviVirgolette(username))
            {
                UtenteEsistente.Username = FunzioniTrasversali.rimuoviVirgolette(username);
                datiModificati = true;
            }

            if (UtenteEsistente.Email != FunzioniTrasversali.rimuoviVirgolette(email))
            {
                UtenteEsistente.Email = FunzioniTrasversali.rimuoviVirgolette(email);
                datiModificati = true;
            }
            if (datiModificati)
            {
                UtenteEsistente.dataAggiornamento = DateTime.Now;
                ViewBag.Message = "Dati utente aggiornati con successo.";
                _context.SaveChanges();
            }
            else
            {
                ViewBag.Message = "Nessuna modifica rilevata nei dati dell'utente.";
            }    

            return RedirectToAction("Show");
        }

        // Funzione 4: Funzione che reimposta la password da ADMIN

        public IActionResult updatePassword(int id, string password)
        {
            // 1) Verifico se esiste una sessione ed il ruolo è ADMIN
            if (!VerificaSessione("ADMIN"))
            {
                logFile.LogWarning($"Tentativo di accesso non autorizzato alla reimpostazione della password da parte di {username}");
                ViewBag.Message = "Utente non autorizzato ad effetuare questa operazione";
                return RedirectToAction("Index", "Home");
            }

            // 2) Verifico che la password non sia vuota
            if (string.IsNullOrEmpty(password))
            {
                ViewBag.Message = "La password non può essere vuota!";
                return RedirectToAction("Modifica", "Account", new { id = id });
            }

            // 3) Verifico l'esistenza dell'utente

            var utente = _context.Users.FirstOrDefault(s => s.id == id);
            if (utente == null)
            {
                ViewBag.Message = "Utente non trovato!";
                return RedirectToAction("Show");
            }

            // 4) Verifico che la password rispetti i criteri minimi (di lunghezza minima 8 caratteri, almeno una lettera maiuscola, una minuscola, un numero e un carattere speciale)
            // if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
            // {
            //     ViewBag.Message = "La password non soddisfa i criteri di sicurezza. Deve contenere almeno 8 caratteri, una lettera maiuscola, una minuscola, un numero e un carattere speciale.";
            //     return RedirectToAction("Modifica", "Account", new { id = id });
            // }

            // 5) Crittografo la password con PasswordHasher
            var hasher = new PasswordHasher<User>();
            string hashedPassword = hasher.HashPassword(utente, password);
            string newPassword = hashedPassword;

            // 6) Salvo le modifiche
            utente.Password = newPassword;
            // utente.dataAggiornamento = DateTime.Now;
            utente.DataAggiornamentoPassword = DateTime.Now;

            _context.Users.Update(utente);
            _context.SaveChanges();
            logFile.LogInfo($"Password reimpostata per l'utente {utente.Username} da parte di {HttpContext.Session.GetString("Username")}");

            ViewBag.Message = "Password Cambiata con successo!";
            return RedirectToAction("Modifica", "Account", new { id = id });

        }

        // Funzione 5: Funzione che reimposta la password di un user

        public IActionResult ChangePassword(int id, string password, string newPassword, string confirmPassword)
        {
            if (!VerificaSessione())
            {
                logFile.LogWarning("Utente non autorizzato ad accedere alla funzione di cambio password.");
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }
            
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

            // 4. Crittografo la nuova password con PasswordHasher
            var hasher = new PasswordHasher<User>();
            string hashedPassword = hasher.HashPassword(utente, newPassword);
        
            // 5. Aggiorno la password

            utente.Password = hashedPassword;
            // utente.dataAggiornamento = DateTime.Now;
            utente.DataAggiornamentoPassword = DateTime.Now;

            _context.Users.Update(utente);
            _context.SaveChanges();

            // 6. Registro l'evento
            logFile.LogInfo($"Password cambiata per l'utente {utente.Username} ");

            ViewBag.Message = "Password Cambiata con successo!";
            return RedirectToAction("Dettagli", "Account", new { id = id });
        }

        // Fine - Funzioni
    }
}