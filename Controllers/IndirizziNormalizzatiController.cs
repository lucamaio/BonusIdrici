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
        private readonly FileLog _logIndirizzi = new FileLog("wwwroot/log/IndirizziNormalizzati.log");

         private string? ruolo;
        private int? idUser;
        private string? username;

        public IndirizziNormalizzatiController(ILogger<IndirizziNormalizzatiController> logger, ApplicationDbContext context, AppCacheService cache, IndirizziService indirizziService)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
            _indirizziService = indirizziService;

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
            List<IndirizzoNormalizzato> indirizziNormalizzati = _context.IndirizzoNormalizzato
                .AsNoTracking()
                .Where(i => _context.ViaEnte.Any(v => v.IdIndirizzoNormalizzato == i.id && v.IdEnte == selectedEnteId))
                .ToList();

            ViewBag.selectedEnteId = selectedEnteId;
            ViewBag.IndirizziNormalizzati = indirizziNormalizzati;
            ViewBag.VieEnte = IsAdminPrincipale()
                ? _context.ViaEnte.AsNoTracking()
                    .Where(v => v.IdEnte == selectedEnteId)
                    .OrderBy(v => v.denominazione)
                    .ThenBy(v => v.tipoVia)
                    .ToList()
                : new List<VieEnte>();
            ViewBag.NomeEnte = _context.Enti.Where(e => e.id == selectedEnteId).Select(e => e.nome).FirstOrDefault() ?? "N/D";

            return View("Show");

        }


        public IActionResult PopolaVie(int id)
        {
            if (!VerificaSessione("ADMIN") || !IsAdminPrincipale())
            {
                _logIndirizzi.LogWarning($"Utente non autorizzato. Tentativo di generazione vie ente. IdEnte: {id}, Utente: {username}, IdUser: {idUser}");

                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // 1. Verifico che l'ente selezionato sia valido
            if (id <= 0 || !_context.Enti.Any(e => e.id == id))
            {
                _logIndirizzi.LogWarning($"Tentativo di generazione indirizzi con ente non valido. IdEnte: {id}, Utente: {username}, IdUser: {idUser}");

                ViewBag.Enti = _cache.GetOrCreate(
                    "enti:all",
                    () => _context.Enti.AsNoTracking().OrderBy(e => e.nome).ToList(),
                    AppCacheService.EntiExpiration
                );

                ViewBag.Message = "Per favore, seleziona un ente valido.";
                return View("Index");
            }

            try
            {
                _logIndirizzi.LogInfo($"Avvio generazione vie ente. IdEnte: {id}, Utente: {username}, IdUser: {idUser}");

                // 2. Mi ricavo i toponimi relativi all'ente selezionato
                var risultati = _indirizziService.RicavaViePerEnte(id);

                // 3. Log del risultato
                if (risultati == null)
                {
                    _logIndirizzi.LogError($"La funzione RicavaViePerEnte ha restituito NULL. IdEnte: {id}, Utente: {username}, IdUser: {idUser}");

                    ViewBag.Message = "Errore durante la generazione degli indirizzi normalizzati.";
                    return RedirectToAction("Index");
                }

                if (risultati.Count == 0)
                {
                    _logIndirizzi.LogWarning($"La funzione RicavaViePerEnte non ha restituito alcun indirizzo. IdEnte: {id}, Utente: {username}, IdUser: {idUser}");
                }
                else
                {
                    _logIndirizzi.LogInfo($"Generazione indirizzi normalizzati completata correttamente. IdEnte: {id}, NumeroIndirizzi: {risultati.Count}, Utente: {username}, IdUser: {idUser}");

                    foreach (var via in risultati)
                    {
                        _logIndirizzi.LogInfo($"Indirizzo generato - Id: {via.id}, Denominazione: {via.denominazione}, TipoVia: {via.tipoVia}, IdEnte: {via.IdEnte}, IdIndirizzoNormalizzato: {via.IdIndirizzoNormalizzato}");
                    }

                    using var transaction = _context.Database.BeginTransaction();

                    var indirizziEsistenti = _context.IndirizzoNormalizzato
                        .ToList()
                        .GroupBy(i => NormalizzaChiave(i.denominazione))
                        .ToDictionary(g => g.Key, g => g.First());

                    var vieDaSalvare = risultati
                        .Where(v => !string.IsNullOrWhiteSpace(v.denominazione))
                        .GroupBy(v => $"{NormalizzaChiave(v.denominazione)}|{v.tipoVia.ToUpperInvariant()}")
                        .Select(g => g.First())
                        .ToList();

                    foreach (var via in vieDaSalvare)
                    {
                        var chiaveIndirizzo = NormalizzaChiave(via.denominazione);

                        if (!indirizziEsistenti.ContainsKey(chiaveIndirizzo))
                        {
                            var nuovoIndirizzo = new IndirizzoNormalizzato(via.denominazione.Trim().ToUpperInvariant(), "Da verificare");
                            _context.IndirizzoNormalizzato.Add(nuovoIndirizzo);
                            indirizziEsistenti.Add(chiaveIndirizzo, nuovoIndirizzo);
                        }
                    }

                    _context.SaveChanges();

                    var vieEsistenti = _context.ViaEnte
                        .Where(v => v.IdEnte == id)
                        .ToList()
                        .Select(v => $"{NormalizzaChiave(v.denominazione)}|{v.tipoVia.ToUpperInvariant()}")
                        .ToHashSet();

                    var vieAggiunte = 0;

                    foreach (var via in vieDaSalvare)
                    {
                        var chiaveVia = $"{NormalizzaChiave(via.denominazione)}|{via.tipoVia.ToUpperInvariant()}";

                        if (vieEsistenti.Contains(chiaveVia))
                        {
                            continue;
                        }

                        var indirizzoNormalizzato = indirizziEsistenti[NormalizzaChiave(via.denominazione)];

                        if (!indirizzoNormalizzato.id.HasValue)
                        {
                            continue;
                        }

                        via.IdIndirizzoNormalizzato = indirizzoNormalizzato.id.Value;
                        via.denominazione = via.denominazione.Trim().ToUpperInvariant();
                        via.dataCreazione = DateTime.Now;
                        via.dataAggiornamento = null;

                        _context.ViaEnte.Add(via);
                        vieEsistenti.Add(chiaveVia);
                        vieAggiunte++;
                    }

                    _context.SaveChanges();
                    transaction.Commit();

                    _logIndirizzi.LogInfo($"Salvate {vieAggiunte} nuove vie sul DB per IdEnte: {id}");
                }

                TempData["Message"] = "Generazione delle vie ente completata correttamente.";

                return RedirectToAction("Show", new { selectedEnteId = id });
            }
            catch (Exception ex)
            {
                _logIndirizzi.LogError($"Errore durante l'esecuzione di RicavaViePerEnte. IdEnte: {id}, Utente: {username}, IdUser: {idUser}, Errore: {ex.Message}");
                _logger.LogError(
                    ex,
                    "Errore durante l'esecuzione di RicavaViePerEnte. IdEnte: {IdEnte}, Utente: {Username}, IdUser: {IdUser}",
                    id,
                    username,
                    idUser
                );

                ViewBag.Message = "Si è verificato un errore durante la generazione degli indirizzi normalizzati.";
                return RedirectToAction("Index");
            }
        }

        private static string NormalizzaChiave(string? valore)
        {
            return string.Join(" ", (valore ?? string.Empty).Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }


    
}
