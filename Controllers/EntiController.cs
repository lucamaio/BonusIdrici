using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;
using System.IO;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using BonusIdrici2.Models.ViewModels; 

namespace BonusIdrici2.Controllers
{
    public class EntiController : Controller
    {
        private readonly ILogger<EntiController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext

         private string? ruolo;
        private int idUser;
        private string? username;

        public EntiController(ILogger<EntiController> logger, ApplicationDbContext context)
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

        // Pagina home che consente la selezione del ente
        public IActionResult Index()
        {
            if (!VerificaSessione("ADMIN"))
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.idUser = HttpContext.Session.GetString("idUser");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Ruolo = HttpContext.Session.GetString("Role");

            var dati = _context.Enti.ToList();
            var viewModelList = dati.Select(x => new EntiViewModel
            {
                id = x.id,
                nome = x.nome,
                istat = x.istat,
                partitaIva = x.partitaIva,
                CodiceFiscale = x.CodiceFiscale,
                Cap = x.Cap,
                Provincia = x.Provincia,
                Regione = x.Regione,
            }).ToList();

            return View("Index", viewModelList);
        }

        // Pagina per la creazione di un nuovo Ente
        public IActionResult Create()
        {
            if (!VerificaSessione("ADMIN"))
            {
                 ViewBag.Message = "Utente non autorizzato ad creare un nuovo ente";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.idUser = HttpContext.Session.GetString("idUser");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Ruolo = HttpContext.Session.GetString("Role");

            return View();
        }

        // Pagina 3: Pagina per la modificha dei dati di un ente

        public IActionResult Modifica(int id, string nome, string istat, string partitaIva, string cap, string? CodiceFiscale, string? provincia, string? regione, bool? Nostro = true)
        {
            if (!VerificaSessione("ADMIN"))
            {
                 ViewBag.Message = "Utente non autorizzato alla modifica dei dati del ente";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.idUser = HttpContext.Session.GetString("idUser");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Ruolo = HttpContext.Session.GetString("Role");

            ViewBag.id = id;
            ViewBag.nome = nome;
            ViewBag.istat = istat;
            ViewBag.partitaIva = partitaIva;
            ViewBag.Cap = cap;
            ViewBag.provincia = provincia;
            ViewBag.regione = regione;
            ViewBag.CodiceFiscale = CodiceFiscale;
            ViewBag.Nostro = Nostro;
            return View();
        }

        // Fine - Pagine di Navigazione

        // Inizio - Funzioni da eseguire a seconda della operazione

        // Funzione che viene eseguita dopo aver compilato il form per la creazione di un ente

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpPost]
        public IActionResult Crea(Ente ente)
        {
            if (ModelState.IsValid)
            {
                // Logica per salvare l'ente nel database
                _context.Enti.Add(ente);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Ente creato con successo.";
                return RedirectToAction("Index", "Home"); // oppure una vista di conferma
            }
            // In caso di errore, ritorna la stessa vista con il modello
            return View("Index", "Enti");
        }
        
        // Funzione che viene eseguita per aggiornare i dati del ente con queli inseriti nel form

        [HttpPost]
        public IActionResult Update(int id, string nome, string istat, string partitaIva, string cap, string? CodiceFiscale, string? provincia, string? regione, bool? Nostro)
        {
            var enteEsistente = _context.Enti.FirstOrDefault(s => s.id == id);

            if (enteEsistente == null)
            {
                return RedirectToAction("Index", "Home");
            }

            enteEsistente.nome = nome.Trim().ToUpper();
            enteEsistente.istat = istat.Trim().ToUpper();
            enteEsistente.partitaIva = partitaIva.Trim().ToUpper();
            enteEsistente.Cap = cap.Trim();
            enteEsistente.Provincia = provincia.Trim().ToUpper();
            enteEsistente.Regione = regione.Trim().ToUpper();
            enteEsistente.CodiceFiscale = CodiceFiscale.Trim().ToUpper();
            enteEsistente.Nostro = Nostro;
            enteEsistente.data_aggiornamento = DateTime.Now;

            _context.SaveChanges();
            return RedirectToAction("Index", "Enti");
        }
        
        // Fine - Funzioni da eseguire a seconda della operazione

    }
}