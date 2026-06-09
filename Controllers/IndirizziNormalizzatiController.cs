using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using Models; 
using Data;
using System.IO;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Models.ViewModels; 
using BonusIdrici2.Services;
using Microsoft.EntityFrameworkCore;

namespace Controllers
{
    public class IndirizziNormalizzatiController : Controller
    {
        private readonly ILogger<IndirizziNormalizzatiController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext
        private readonly AppCacheService _cache;
        private readonly IndirizziService _indirizziService;
        private readonly VieEnteService _vieEnteService;
        private readonly SectionActivityService _sectionActivityService;
        private readonly FileLog _logIndirizzi = new FileLog("wwwroot/log/IndirizziNormalizzati.log");

         private string? ruolo;
        private int? idUser;
        private string? username;

        public IndirizziNormalizzatiController(ILogger<IndirizziNormalizzatiController> logger, ApplicationDbContext context, AppCacheService cache, IndirizziService indirizziService, VieEnteService vieEnteService, SectionActivityService sectionActivityService)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
            _indirizziService = indirizziService;
            _vieEnteService = vieEnteService;
            _sectionActivityService = sectionActivityService;

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

        private bool IsAdminPrincipale()
        {
            return string.Equals(ruolo, "ADMIN", StringComparison.OrdinalIgnoreCase) && idUser == 1;
        }

        // Pagina 1 

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
                enti = FunzioniTrasversali.GetEnti(_context, idUser);
                if (enti.Count == 1)
                {
                    return Show(enti[0].id);
                }
            }

            enti = _cache.GetOrCreate(
                "enti:all",
                () => _context.Enti.AsNoTracking().OrderBy(e => e.nome).ToList(),
                AppCacheService.EntiExpiration);
            ViewBag.Enti = enti;
            return View();
        }

         public IActionResult Show(int selectedEnteId)
        {
            // Verifico l'autorizzazione e la sessione dell'utente
            if (!VerificaSessione())
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad accedere a questa pagina. Ha invocato la pagina di visualizzazione toponimi per l'ente ID " + selectedEnteId);
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // Verifico che l'ente selezionato sia valido

            if (selectedEnteId == 0)
            {
                ViewBag.Enti = _cache.GetOrCreate(
                    "enti:all",
                    () => _context.Enti.AsNoTracking().OrderBy(e => e.nome).ToList(),
                    AppCacheService.EntiExpiration);
                ViewBag.Message = "Per favore, seleziona un ente valido.";
                return View("Index", "Toponomi");
            }
            // Mi ricavo gli indirizzi normalizzati relativi all'ente selezionato
            List<IndirizzoNormalizzato> indirizziNormalizzati = _context.IndirizziNormalizzati
                .AsNoTracking()
                .Where(i => i.IdEnte == selectedEnteId)
                .OrderBy(i => i.DenominazioneNormalizzata)
                .ToList();

            var vieEnte = IsAdminPrincipale()
                ? _context.VieEnte.AsNoTracking()
                    .Where(v => v.IdEnte == selectedEnteId)
                    .OrderBy(v => v.DenominazionePulita)
                    .ThenBy(v => v.Fonte)
                    .ToList()
                : new List<VieEnte>();

            ViewBag.selectedEnteId = selectedEnteId;
            ViewBag.IndirizziNormalizzati = indirizziNormalizzati;
            ViewBag.VieEnte = vieEnte;
            ViewBag.TotaleVieEnte = vieEnte.Count;
            ViewBag.VieDaAnalizzare = vieEnte.Count(v => v.Stato == "DA_ANALIZZARE" || v.Stato == "PROPOSTA");
            ViewBag.VieCollegate = vieEnte.Count(v => v.Stato == "COLLEGATA");
            ViewBag.VieAmbigue = vieEnte.Count(v => v.Stato == "AMBIGUA");
            ViewBag.TotaleIndirizziNormalizzati = indirizziNormalizzati.Count;
            ViewBag.NomeEnte = _context.Enti.Where(e => e.id == selectedEnteId).Select(e => e.nome).FirstOrDefault() ?? "N/D";
            ViewBag.SectionActivity = _sectionActivityService.GetIndirizziNormalizzatiActivity(selectedEnteId);

            return View("Show");

        }

        public IActionResult Modifica(int id)
        {
            if (!VerificaSessione())
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad accedere alla modifica indirizzo normalizzato ID " + id);
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            var indirizzoNormalizzato = _context.IndirizziNormalizzati
                .AsNoTracking()
                .FirstOrDefault(i => i.Id == id);

            if (indirizzoNormalizzato == null)
            {
                TempData["Message"] = "Indirizzo normalizzato non trovato.";
                return RedirectToAction("Index");
            }

            var vieCollegate = _context.VieEnte
                .AsNoTracking()
                .Where(v => v.IdEnte == indirizzoNormalizzato.IdEnte && v.IdIndirizzoNormalizzato == indirizzoNormalizzato.Id)
                .OrderBy(v => v.DenominazionePulita)
                .ThenBy(v => v.DenominazioneOriginale)
                .ThenBy(v => v.Fonte)
                .ToList();

            ViewBag.NomeEnte = _context.Enti
                .AsNoTracking()
                .Where(e => e.id == indirizzoNormalizzato.IdEnte)
                .Select(e => e.nome)
                .FirstOrDefault() ?? "N/D";
            ViewBag.VieCollegate = vieCollegate;

            return View("Modifica", indirizzoNormalizzato);
        }

        [HttpPost]
        public async Task<IActionResult> Update(int id, int idEnte, string denominazioneNormalizzata, bool attivo, string? note)
        {
            if (!VerificaSessione())
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad aggiornare indirizzo normalizzato ID " + id);
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            var indirizzoNormalizzato = await _context.IndirizziNormalizzati
                .FirstOrDefaultAsync(i => i.Id == id && i.IdEnte == idEnte);

            if (indirizzoNormalizzato == null)
            {
                TempData["Message"] = "Indirizzo normalizzato non trovato.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(denominazioneNormalizzata))
            {
                TempData["Message"] = "La denominazione normalizzata e obbligatoria.";
                return RedirectToAction("Modifica", new { id });
            }

            indirizzoNormalizzato.DenominazioneNormalizzata = denominazioneNormalizzata.Trim().ToUpperInvariant();
            indirizzoNormalizzato.Attivo = attivo;
            indirizzoNormalizzato.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            indirizzoNormalizzato.DataAggiornamento = DateTime.Now;
            indirizzoNormalizzato.IdUser = idUser ?? indirizzoNormalizzato.IdUser;

            await _context.SaveChangesAsync();
            _cache.ClearEnteCache(idEnte);

            TempData["Message"] = "Indirizzo normalizzato aggiornato correttamente.";
            return RedirectToAction("Show", new { selectedEnteId = idEnte });
        }


        [HttpPost]
        public async Task<IActionResult> PopolaVieEnte(int selectedEnteId)
        {
            if (!VerificaSessione("ADMIN") || !IsAdminPrincipale())
            {
                _logIndirizzi.LogWarning($"Utente non autorizzato. Tentativo di popolamento VieEnte. IdEnte: {selectedEnteId}, Utente: {username}, IdUser: {idUser}");
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            if (selectedEnteId <= 0 || !await _context.Enti.AnyAsync(e => e.id == selectedEnteId))
            {
                TempData["Message"] = "Per favore, seleziona un ente valido.";
                return RedirectToAction("Index");
            }

            try
            {
                var result = await _vieEnteService.PopolaVieEnteAsync(selectedEnteId, idUser ?? 0);
                _cache.ClearEnteCache(selectedEnteId);
                TempData["Message"] = $"VieEnte popolate. Analizzate: {result.TotaleAnalizzate}, Nuove: {result.Nuove}, Aggiornate: {result.Aggiornate}, Civici estratti: {result.ConCivicoEstratto}.";
                return RedirectToAction("Show", new { selectedEnteId });
            }
            catch (Exception ex)
            {
                _logIndirizzi.LogError($"Errore durante PopolaVieEnte. IdEnte: {selectedEnteId}, Utente: {username}, IdUser: {idUser}, Errore: {ex.Message}");
                _logger.LogError(ex, "Errore durante PopolaVieEnte. IdEnte: {IdEnte}, Utente: {Username}, IdUser: {IdUser}", selectedEnteId, username, idUser);
                TempData["Message"] = "Si e verificato un errore durante il popolamento delle VieEnte.";
                return RedirectToAction("Show", new { selectedEnteId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreaIndirizziNormalizzati(int selectedEnteId)
        {
            if (!VerificaSessione("ADMIN") || !IsAdminPrincipale())
            {
                _logIndirizzi.LogWarning($"Utente non autorizzato. Tentativo di creazione IndirizziNormalizzati. IdEnte: {selectedEnteId}, Utente: {username}, IdUser: {idUser}");
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            if (selectedEnteId <= 0 || !await _context.Enti.AnyAsync(e => e.id == selectedEnteId))
            {
                TempData["Message"] = "Per favore, seleziona un ente valido.";
                return RedirectToAction("Index");
            }

            try
            {
                var result = await _vieEnteService.CreaIndirizziNormalizzatiAsync(selectedEnteId, idUser ?? 0);
                _cache.ClearEnteCache(selectedEnteId);
                TempData["Message"] = $"Indirizzi normalizzati creati. Gruppi: {result.GruppiAnalizzati}, Creati: {result.IndirizziCreati}, Vie collegate: {result.VieCollegate}, Abbreviazioni collegate: {result.VieAbbreviateCollegate}, Ambigue: {result.VieAmbigue}.";
                return RedirectToAction("Show", new { selectedEnteId });
            }
            catch (Exception ex)
            {
                _logIndirizzi.LogError($"Errore durante CreaIndirizziNormalizzati. IdEnte: {selectedEnteId}, Utente: {username}, IdUser: {idUser}, Errore: {ex.Message}");
                _logger.LogError(ex, "Errore durante CreaIndirizziNormalizzati. IdEnte: {IdEnte}, Utente: {Username}, IdUser: {IdUser}", selectedEnteId, username, idUser);
                TempData["Message"] = "Si e verificato un errore durante la creazione degli indirizzi normalizzati.";
                return RedirectToAction("Show", new { selectedEnteId });
            }
        }
    }


    
}
