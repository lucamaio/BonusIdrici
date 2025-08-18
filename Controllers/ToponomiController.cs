using Microsoft.AspNetCore.Mvc;
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
    public class ToponomiController : Controller
    {
        private readonly ILogger<ToponomiController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext
        public ToponomiController(ILogger<ToponomiController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Pagine di navigazione

        public IActionResult Index()
        {
             if (!VerificaSessione())
            {
                //ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            var ruolo = HttpContext.Session.GetString("Role");
            int idUser = (int)HttpContext.Session.GetInt32("idUser");
            List<Ente> enti = new List<Ente>();

            if (ruolo == "OPERATORE")
            {
                enti = FunzioniTrasversali.GetEnti(_context, idUser);
                if (enti.Count == 1)
                {
                    return Show(enti[0].id);
                }
            }

            enti = _context.Enti.OrderBy(e => e.nome).ToList();
            ViewBag.Enti = enti;
            return View();
        }

        public IActionResult Create(int idEnte)
        {
            ViewBag.IdEnte = idEnte;
            return View();
        }

        public IActionResult Modifica(int id, string denominazione, string? normalizzazione, DateTime data_creazione, int idEnte)
        {
            ViewBag.id = id;
            ViewBag.denominazione = denominazione;
            ViewBag.normalizzazione = normalizzazione;
            ViewBag.data_creazione = data_creazione;
            ViewBag.IdEnte = idEnte;
            return View();
        }


        public IActionResult Show(int selectedEnteId)
        {
            if (selectedEnteId == 0)
            {
                ViewBag.Enti = _context.Enti.OrderBy(e => e.nome).ToList();
                ViewBag.Message = "Per favore, seleziona un ente valido.";
                return View("Index", "Toponomi");
            }

            var dati = _context.Toponomi
                        .Where(r => r.IdEnte == selectedEnteId)
                        .OrderByDescending(x => x.denominazione)
                        .ToList();

            var viewModelList = dati.Select(x => new ToponomiViewModel
            {
                id = x.id,
                denominazione = x.denominazione,
                normalizzazione = x.normalizzazione,
                data_creazione = x.data_creazione,
                data_aggiornamento = x.data_aggiornamento,
                IdEnte = x.IdEnte
            }).ToList();

            ViewBag.SelectedEnteId = selectedEnteId;
            ViewBag.SelectedEnteNome = _context.Enti.FirstOrDefault(e => e.id == selectedEnteId)?.nome ?? "Ente Sconosciuto";

            return View("Show", viewModelList);
        }

        [HttpPost]
        public IActionResult crea(string denominazione, string normalizzazione, int idEnte)
        {
            var nuovoToponimo = new Toponimo
            {
                denominazione = denominazione.Trim().ToUpper(),
                normalizzazione = normalizzazione.Trim().ToUpper(),
                IdEnte = idEnte,
                data_creazione = DateTime.Now,
                data_aggiornamento = null
            };

            _context.Toponomi.Add(nuovoToponimo);
            _context.SaveChanges();

            return RedirectToAction("Show", "Toponomi", new { selectedEnteId = idEnte });
        }

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
            toponimoEsistente.data_aggiornamento = DateTime.Now;
            // data_creazione non viene modificata
            toponimoEsistente.IdEnte = idEnte;

            _context.SaveChanges();

            return RedirectToAction("Show", "Toponomi", new { selectedEnteId = idEnte });
        }

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