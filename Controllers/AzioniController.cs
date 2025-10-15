using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
 using System.Globalization;
using Models;
using Data;
using Models.ViewModels; // Aggiungi questo using
using System.IO;

namespace Controllers
{
    public class AzioniController : Controller
    {
        private readonly ILogger<AzioniController> _logger;
        private readonly ApplicationDbContext _context;

        private string? ruolo;
        private int idUser;
        private string? username;

        public AzioniController(ILogger<AzioniController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
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

        // Pagina 1: Consente di andare a caricare i dati di un ente di cui si vogliono generare i vari domande
        public IActionResult LoadFileINPS()
        {
            // 1. Verifico se esiste una sessione attiva e che il ruolo del utente è ADMIN
            if (!VerificaSessione())
            {
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

        // Fine - Pagnie di Navigazione

        // Inizio - Funzioni
        
        // Funzione 1: consente di caricare il file fornito mensilmente dal INPS per generare i Domande

        [HttpPost]
        public async Task<IActionResult> LoadFileINPS(IFormFile csv_file, int selectedEnteId, string mese, string anno, int serie, bool confrontoCivico, bool escludiComponenti)
        {
            // Verifico se i dati non sono null
            if (string.IsNullOrEmpty(ruolo) || string.IsNullOrEmpty(username) || idUser == 0)
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }


            if (string.IsNullOrEmpty(mese) || string.IsNullOrEmpty(anno))
            {
                ViewBag.Message = "Dati mese e anno mancanti!";
                 return RedirectToAction("LoadFileINPS");
            }

            if (csv_file == null || csv_file.Length == 0)
            {
                ViewBag.Message = "Seleziona un file CSV da caricare.";
                ViewBag.Enti = _context.Enti.ToList();
                return RedirectToAction("LoadFileINPS");
            }

            if (csv_file == null || csv_file.Length == 0)
            {
                ViewBag.Message = "Seleziona un file CSV da caricare.";
                return RedirectToAction("LoadFileINPS");
            }

            // Validazione del tipo di file (opzionale ma consigliata)
            if (Path.GetExtension(csv_file.FileName).ToLowerInvariant() != ".csv")
            {
                ViewBag.Message = "Il file selezionato non è un CSV valido.";
                return RedirectToAction("LoadFileINPS");
            }

            var selectedEnte = await _context.Enti.FindAsync(selectedEnteId);
            if (selectedEnte == null)
            {
                ViewBag.Message = "Ente selezionato non valido.";
                ViewBag.Enti = _context.Enti.ToList();
                return RedirectToAction("LoadFileINPS");
            }
            

            string filePath = Path.GetTempFileName(); // Crea un file temporaneo

            try
            {
                // Salva il file caricato su disco
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await csv_file.CopyToAsync(stream);
                }

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
                // Leggi il file CSV con la tua classe CSVReaders
                var datiComplessivi = CSVReader.LeggiFileINPS(filePath, _context, selectedEnteId, idReport, confrontoCivico, escludiComponenti);
                AccountController.logFile.LogInfo($"L'utente {username} ha effetuato un nuova elaborazione del file csv del INPS per l'ente {selectedEnteId}");
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        bool datiSalvati = false;

                        if (datiComplessivi.domande.Count > 0)
                        {
                            datiSalvati = true;
                            foreach (var domande in datiComplessivi.domande)
                            {
                                _context.Domande.Add(domande);
                            }
                            await _context.SaveChangesAsync();
                        }

                        if (datiComplessivi.domandeDaAggiornare.Count > 0)
                        {
                            datiSalvati = true;
                            foreach (var domande in datiComplessivi.domandeDaAggiornare)
                            {
                                _context.Domande.Update(domande);
                            }
                            await _context.SaveChangesAsync();
                        }

                        if (!datiSalvati)
                        {
                            ViewBag.Message = "Nessun dato da salvare.";
                            return RedirectToAction("LoadFileINPS");
                        }

                        transaction.Commit(); // Conferma la transazione se tutto è andato bene
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

             return RedirectToAction("LoadFileINPS"); 
        }
        // Fine - Funzioni
    }
}
