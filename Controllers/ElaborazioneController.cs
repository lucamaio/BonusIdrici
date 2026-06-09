using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
 using System.Globalization;
using Models;
using Data;
using Models.ViewModels; // Aggiungi questo using
using System.IO;
using System.Text.RegularExpressions;
using BonusIdrici2.Services;

namespace Controllers
{
    public class elaborazioneController : Controller
    {
        private readonly ILogger<elaborazioneController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly AppCacheService _cache;
        private readonly INPSReaderNormalizzatoService _inpsReaderNormalizzatoService;

        private string? ruolo;
        private int idUser;
        private string? username;

        public elaborazioneController(ILogger<elaborazioneController> logger, ApplicationDbContext context, AppCacheService cache, INPSReaderNormalizzatoService inpsReaderNormalizzatoService)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
            _inpsReaderNormalizzatoService = inpsReaderNormalizzatoService;
        }

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

        // Funzione di validazione
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

        // Inizio - Pagine di Navigazione

        // Pagina 1: Pagina che consente di caricare i dati per effetuare una nuova elaborazione dei dati forniti dal INPS

        public IActionResult NewProcessing()
        {
            // 1. Verifico se esiste una sessione attiva e che il ruolo del utente è ADMIN
            if (!VerificaSessione())
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di nuova elaborazione dati INPS.");
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }
            // Mi ricavo i mesi

            var mesi = DateTimeFormatInfo.CurrentInfo.MonthNames.Where(m => !string.IsNullOrEmpty(m)).ToList();
            ViewBag.Mesi = mesi;
            // 2) Mi ricavo gli enti
            List<Ente> enti = new List<Ente>();

            if (ruolo == "OPERATORE")
            {
                // 2.a) Verifico se gestisce un solo ente
                enti = FunzioniTrasversali.GetEnti(_context, idUser);
                if (enti.Count == 1)
                {
                    ViewBag.Enti = enti;
                    return View();
                }

                // 2.b)  Altrimenti mostro so gli enti su cui l'utente opera

                ViewBag.Enti = enti;
                return View();
            }
            // 2.c) Se sono amministratore me li mostri tutti
            enti = _context.Enti.OrderBy(e => e.nome).ToList();
            ViewBag.Enti = enti;

            ViewBag.Enti = _context.Enti.ToList();
            return View();
        }

        // Pagina 2: Pagina che contiene una giuda per comprendere il funzionamento 

        public IActionResult Guida()
        {
            return View();
        }
        
        // Fine - Pagine di Navigazione

        // Inizio - Funzioni

        // Funzione 1: Genera i report e le relative domande

        [HttpPost]
        public async Task<IActionResult> Processing(IFormFile csv_file, int selectedEnteId, string mese, string anno, int serie, bool confrontoCivico, bool escludiComponenti, bool escludiAlertSnapshot)
        {
            // Fase 0: Verifico se esiste una sessione
            if (!VerificaSessione())
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad accedere alla pagina di elaborazione dati INPS. Ha invocato la funzione Processing dei dati.");
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // Fase 1: Verifico se i dati ricevuti sono non vuoti

            if (string.IsNullOrEmpty(ruolo) || string.IsNullOrEmpty(username) || idUser == 0)
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }


            if (string.IsNullOrEmpty(mese) || string.IsNullOrEmpty(anno))
            {
                ViewBag.Message = "Dati mese e anno mancanti!";
                 return RedirectToAction("NewProcessing");
            }

            if (csv_file == null || csv_file.Length == 0)
            {
                ViewBag.Message = "Seleziona un file CSV da caricare.";
                ViewBag.Enti = _context.Enti.ToList();
                return RedirectToAction("NewProcessing");
            }

            // Validazione del tipo di file
            if (Path.GetExtension(csv_file.FileName).ToLowerInvariant() != ".csv")
            {
                ViewBag.Message = "Il file selezionato non è un CSV valido.";
                return RedirectToAction("NewProcessing");
            }

            // Verifico l'ente selezionato
            var selectedEnte = await _context.Enti.FindAsync(selectedEnteId);
            if (selectedEnte == null)
            {
                ViewBag.Message = "Ente selezionato non valido.";
                ViewBag.Enti = _context.Enti.ToList();
                return RedirectToAction("NewProcessing");
            }
            
            // Fase 2: Elaborazione

            string filePath = Path.GetTempFileName(); // Crea un file temporaneo

            try
            {
                // Salva il file caricato su disco
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await csv_file.CopyToAsync(stream);
                }

                var (annoReport, meseReport) = DeterminaPeriodoReport(csv_file.FileName, mese, anno);

                // Verifico se non esiste alcun report con i dati passati
                var reportEsistente = _context.Reports.FirstOrDefault(f=> f.mese == mese && f.anno == anno && f.idEnte==selectedEnteId);
                int idReport;
                if(reportEsistente != null){
                    // Aggiungi parte aggiornamento report
                    idReport = reportEsistente.id;
                }else{
                    var report = new Report(){
                        mese = mese,
                        anno = anno,
                        stato ="Da verificare",
                        serie = serie,
                        idUser = idUser,
                        idEnte = selectedEnteId,
                        DataCreazione = DateTime.Now
                    };

                    _context.Reports.Add(report);
                    await _context.SaveChangesAsync();
                    
                    idReport = report.id;
                }            
                /*
                 * LOGICA LEGACY INPS - MANTENUTA COME FALLBACK
                 *
                 * La vecchia lettura INPS tramite CSVReader.LeggiFileINPS resta
                 * commentata per poter tornare rapidamente alla precedente elaborazione.
                 * La nuova elaborazione usa INPSReaderNormalizzatoService e sostituisce
                 * esclusivamente il confronto indirizzi tramite VieEnte e IndirizziNormalizzati.
                 */
                // var datiComplessivi = CSVReader.LeggiFileINPS(filePath, _context, selectedEnteId, idReport, confrontoCivico, escludiComponenti, annoReport, meseReport);
                var datiComplessivi = await _inpsReaderNormalizzatoService.LeggiFileINPSNormalizzatoAsync(
                    filePath,
                    selectedEnteId,
                    idReport,
                    confrontoCivico,
                    escludiComponenti,
                    escludiAlertSnapshot,
                    annoReport,
                    meseReport);
                AccountController.logFile.LogInfo($"L'utente {username} ha effetuato un nuova elaborazione del file csv del INPS per l'ente {selectedEnteId}");
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Fase 3: Salvo i dati sul DB
                        bool datiSalvati = false;

                        // a) Vertifico se ci sono domande da aggiungere al DB
                        if (datiComplessivi.domande.Count > 0)
                        {
                            datiSalvati = true;
                            foreach (var domande in datiComplessivi.domande)
                            {
                                _context.Domande.Add(domande);
                            }
                            await _context.SaveChangesAsync();
                        }

                        // b) Verifico se ci sono domande da aggiornare al DB
                        if (datiComplessivi.domandeDaAggiornare.Count > 0)
                        {
                            datiSalvati = true;
                            foreach (var domande in datiComplessivi.domandeDaAggiornare)
                            {
                                _context.Domande.Update(domande);
                            }
                            await _context.SaveChangesAsync();
                        }

                        // c) Se non ci sono dati da salvare ritorno alla pagina iniziale
                        
                        if (!datiSalvati)
                        {
                            ViewBag.Message = "Nessun dato da salvare.";
                            return RedirectToAction("NewProcessing");
                        }

                        transaction.Commit(); // Conferma la transazione se tutto è andato bene
                        _cache.ClearReportCache(idReport);
                        _cache.RemoveByPrefix($"reports:ente:{selectedEnteId}");
                        _cache.RemoveByPrefix($"statistiche:ente:{selectedEnteId}");
                        _cache.Remove("dashboard:admin");
                        ViewBag.Message = $"Dati caricati e salvati con successo!\n Aggiunti: {datiComplessivi.domande.Count}, Aggiornati: {datiComplessivi.domandeDaAggiornare.Count}";
                    }
                    catch (Exception dbEx)
                    {
                        transaction.Rollback(); // Annulla la transazione in caso di errore
                        _logger.LogError(dbEx, "Errore durante il salvataggio dei dati nel database.");
                        ViewBag.Message = $"Errore durante il salvataggio dei dati nel database: {dbEx.Message}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione del file CSV.");
                ViewBag.Message = $"Errore durante l'elaborazione del file CSV: {ex.Message}";
            }
            finally
            {
                // Assicurati di eliminare il file temporaneo
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

             return RedirectToAction("NewProcessing"); 
        }
        

        //Fine - Funzioni

        private static (int anno, int mese) DeterminaPeriodoReport(string fileName, string mese, string anno)
        {
            var match = Regex.Match(fileName, @"_BID_(?<anno>\d{4})(?<mese>\d{2})", RegexOptions.IgnoreCase);
            if (match.Success
                && int.TryParse(match.Groups["anno"].Value, out int annoDaFile)
                && int.TryParse(match.Groups["mese"].Value, out int meseDaFile)
                && meseDaFile >= 1
                && meseDaFile <= 12)
            {
                return (annoDaFile, meseDaFile);
            }

            int annoReport = int.TryParse(anno, out int annoDaForm) ? annoDaForm : DateTime.Now.Year;
            int meseReport = DateTimeFormatInfo.CurrentInfo.MonthNames
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select((nome, index) => new { nome, index })
                .FirstOrDefault(m => string.Equals(m.nome, mese, StringComparison.CurrentCultureIgnoreCase))?.index + 1 ?? 0;

            if (meseReport < 1 || meseReport > 12)
            {
                meseReport = DateTime.Now.Month;
            }

            return (annoReport, meseReport);
        }

    }
}
