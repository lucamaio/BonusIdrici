using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Models; 
using Data;
using System.IO;

using Microsoft.AspNetCore.Authentication;
using Models.ViewModels; 

namespace Controllers
{
    public class ToponomiController : Controller
    {
        // Dichiarazione Variabili 
        private readonly ILogger<ToponomiController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext

        private string? ruolo;
        private int? idUser;
        private string? username;

        public ToponomiController(ILogger<ToponomiController> logger, ApplicationDbContext context)
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


        // Inizio Pagine di navigazione
        // Pagina 1: Consente la selezione del ente per effetuare le operazioni successive
        public IActionResult Index()
        {
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            List<Ente> enti = new List<Ente>();

            if (ruolo == "OPERATORE")
            {
                enti = FunzioniTrasversali.GetEnti(_context, (int) idUser);
                if (enti.Count == 1)
                {
                    return Show(enti[0].id);
                }
            }

            enti = _context.Enti.OrderBy(e => e.nome).ToList();
            ViewBag.Enti = enti;
            return View();
        }

        // Pagina 2: Consente la visualizzazione dei toponomi relativi all'ente

        public IActionResult Show(int selectedEnteId)
        {
            // Verifico l'autorizzazione e la sessione dell'utente
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // Verifico che l'ente selezionato sia valido

            if (selectedEnteId == 0)
            {
                ViewBag.Enti = _context.Enti.OrderBy(e => e.nome).ToList();
                ViewBag.Message = "Per favore, seleziona un ente valido.";
                return View("Index", "Toponomi");
            }
            // Mi ricavo i toponimi relativi all'ente selezionato
            var toponimi = _context.Toponomi.Where(r => r.IdEnte == selectedEnteId).OrderByDescending(x => x.denominazione).ToList();

            // Statistiche
            ViewBag.TotaleToponomi = toponimi.Count;
            ViewBag.ToponomiNoNormalizzazione = toponimi.Count(r => r.normalizzazione == null);
            ViewBag.ToponomiNormalizzati = toponimi.Count(r => r.normalizzazione != null);

            // Passo i dati alla view
            ViewBag.Toponimi = toponimi;
            ViewBag.SelectedEnteId = selectedEnteId;
            ViewBag.SelectedEnteNome = _context.Enti.FirstOrDefault(e => e.id == selectedEnteId)?.nome ?? "Ente Sconosciuto";

            return View("Show");
        }

        // Pagina 3: Consente la creazione di un nuovo Toponimo

        public IActionResult Create(int idEnte)
        {
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.IdEnte = idEnte;
            return View();
        }

        // Pagina 4: Consente la modifica dati di un toponimo

        public IActionResult Modifica(int id)
        {
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }
            // 2. Mi ricavo il toponimo

            var top = _context.Toponomi.FirstOrDefault(s => s.id == id);
            if (top == null)
            {
                ViewBag.Message = "Toponimo non trovato!";
                return RedirectToAction("Show", "Toponomi");
            }
            
            ViewBag.Toponimo = top;
            return View();
        }

        // Fine - Pagine Navigazione

        // Inizio - Funzioni da eseguire a seconda della operazione

        //Funzione 1: Consente la creazione di un toponimo una volta che viene inviato il form "Views/Toponomi/Create.cshtml"

        [HttpPost]
        public IActionResult crea(string denominazione, string normalizzazione, int idEnte)
        {
            var nuovoToponimo = new Toponimo
            {
                denominazione = denominazione.Trim().ToUpper(),
                normalizzazione = normalizzazione.Trim().ToUpper(),
                IdEnte = idEnte,
                dataCreazione = DateTime.Now,
                dataAggiornamento = null
            };

            _context.Toponomi.Add(nuovoToponimo);
            _context.SaveChanges();

            return RedirectToAction("Show", "Toponomi", new { selectedEnteId = idEnte });
        }

        // Funzione 2: Consente l'aggiornamento dei dati di un toponimo una volta che il form è stato inviato "Views/Toponomi/Modifica.cshtml"

        [HttpPost]
        public IActionResult Update(int id, string denominazione, string normalizzazione, DateTime data_creazione, int idEnte)
        {
            var toponimoEsistente = _context.Toponomi.FirstOrDefault(t => t.id == id);

            if (toponimoEsistente == null)
            {
                return RedirectToAction("Index", "Home"); // oppure restituisci una view con errore
            }

            // Aggiorna le proprietà
            toponimoEsistente.denominazione = denominazione.Trim().ToUpper();
            toponimoEsistente.normalizzazione = normalizzazione.Trim().ToUpper();
            toponimoEsistente.dataAggiornamento = DateTime.Now;
            // data_creazione non viene modificata
            toponimoEsistente.IdEnte = idEnte;

            _context.SaveChanges();

            return RedirectToAction("Show", "Toponomi", new { selectedEnteId = idEnte });
        }
        
        // Fine - Funzioni da eseguire a seconda della operazione

    }
    
}