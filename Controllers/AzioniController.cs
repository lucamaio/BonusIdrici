using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BonusIdrici2.Models;
using BonusIdrici2.Data;
using System.Globalization;
using BonusIdrici2.Models.ViewModels; // Aggiungi questo using
using System.IO;

namespace BonusIdrici2.Controllers
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

        // Pagine
        public IActionResult LoadAnagrafica()
        {
            if (!VerificaSessione("ADMIN"))
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Enti = _context.Enti.ToList();
            return View();
        }

        public IActionResult LoadFileINPS()
        {
            if (!VerificaSessione("ADMIN"))
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Enti = _context.Enti.ToList();
            return View();
        }

        public IActionResult LoadFilePiranha()
        {
            if (!VerificaSessione("ADMIN"))
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Enti = _context.Enti.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoadAnagrafe(IFormFile csv_file, int selectedEnteId) // Il nome del parametro deve corrispondere al 'name' dell'input file nel form
        {
           // Controllo se l'utente può accedere alla pagina desideratà
            if (string.IsNullOrEmpty(ruolo) || ruolo!="ADMIN")
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            if (csv_file == null || csv_file.Length == 0)
            {
                ViewBag.Message = "Seleziona un file CSV da caricare.";
                return View("LoadAnagrafica"); // Torna alla pagina di upload con un messaggio
            }

            // Validazione del tipo di file (opzionale ma consigliata)
            if (Path.GetExtension(csv_file.FileName).ToLowerInvariant() != ".csv")
            {
                ViewBag.Message = "Il file selezionato non è un CSV valido.";
                return View("LoadAnagrafica");
            }

            var selectedEnte = await _context.Enti.FindAsync(selectedEnteId);

            if (selectedEnte == null)
            {
                ViewBag.Message = "Ente selezionato non valido.";
                ViewBag.Enti = _context.Enti.ToList();
                return View("LoadAnagrafica", "Azioni");
            }
            List<Dichiarante> dichiaranti = _context.Dichiaranti.Where(d => d.IdEnte == selectedEnteId).ToList();
            if (dichiaranti == null)
            {
                dichiaranti = new List<Dichiarante>();
            }
            string filePath = Path.GetTempFileName(); // Crea un file temporaneo

            try
            {
                // Salva il file caricato su disco
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await csv_file.CopyToAsync(stream);
                }

                // Leggi il file CSV con la tua classe CSVReader
                var datiComplessivi = CSVReader.LoadAnagrafe(filePath, selectedEnteId, dichiaranti, idUser);

                // Inizia una transazione per assicurare che tutti i dati vengano salvati
                // o nessuno in caso di errore. (Opzionale ma buona pratica per operazioni multiple)
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        if (datiComplessivi.Dichiaranti.Count > 0)
                        {
                            // Salva i nuovi dichiaranti nel database
                            foreach (var dichiarante in datiComplessivi.Dichiaranti)
                            {
                                _context.Dichiaranti.Add(dichiarante);
                            }
                        }
                        else if (datiComplessivi.DichiarantiDaAggiornare.Count > 0)
                        {
                            // Aggiorna i dichiaranti esistenti
                            foreach (var dichiarante in datiComplessivi.DichiarantiDaAggiornare)
                            {
                                _context.Dichiaranti.Update(dichiarante);
                            }
                        }
                        else
                        {
                            ViewBag.Message = "Nessun dato valido trovato nel file CSV.";
                            return View("LoadAnagrafica"); // Torna alla pagina di upload con un messaggio
                        }
                        if (datiComplessivi.Dichiaranti.Count > 0 || datiComplessivi.DichiarantiDaAggiornare.Count > 0)
                        {
                            // Salva le modifiche al database
                            await _context.SaveChangesAsync();
                        }

                        transaction.Commit(); // Conferma la transazione se tutto è andato bene
                        ViewBag.Message = $"File '{csv_file.FileName}' caricato e dati salvati con successo! Dichiaranti: {datiComplessivi.Dichiaranti.Count}, "; //Atti: {datiComplessivi.Atti.Count}";
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

            return View("LoadAnagrafica"); // Torna alla pagina di upload con il messaggio di stato
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

       [HttpPost]
        public async Task<IActionResult> LoadFilePiranha(IFormFile csv_file, int selectedEnteId)
        {
            // Controllo se l'utente può accedere alla pagina desideratà
            if (string.IsNullOrEmpty(ruolo) || ruolo!="ADMIN")
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // Validazione file
            if (csv_file == null || csv_file.Length == 0)
            {
                ViewBag.Message = "Seleziona un file CSV da caricare.";
                ViewBag.Enti = _context.Enti.ToList();
                return View("LoadFilePiranha", "Azioni");
            }

            if (Path.GetExtension(csv_file.FileName).ToLowerInvariant() != ".csv")
            {
                ViewBag.Enti = _context.Enti.ToList();
                ViewBag.Message = "Il file selezionato non è un CSV valido.";
                return View("LoadFilePiranha", "Azioni");
            }

            // Verifico che l'ente selezionato è valido 
            var selectedEnte = await _context.Enti.FindAsync(selectedEnteId);

            if (selectedEnte == null)
            {
                ViewBag.Message = "Ente selezionato non valido.";
                ViewBag.Enti = _context.Enti.ToList();
                return View("LoadFilePiranha", "Azioni");
            }

            string filePath = Path.GetTempFileName();

            try
            {
                // Salva il file temporaneamente su disco
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await csv_file.CopyToAsync(stream);
                }

                // Carica le utenze esistenti per l’ente selezionato
                List<UtenzaIdrica> utenzeIdriche = _context.UtenzeIdriche.Where(u => u.IdEnte == selectedEnteId).ToList();

                // Lettura del file CSV
                var datiComplessivi = CSVReader.LeggiFilePhirana(filePath, selectedEnteId, utenzeIdriche, _context);

                if (datiComplessivi == null)
                {
                    ViewBag.Message = "Nessun dato valido trovato nel file CSV.";
                    return View("LoadFilePiranha", "Azioni");
                }

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Da Implementare aggiornamento dati UtenzeIdriche
                        
                        // Controllo se sono presenti dei dati da caricare sul DB
                        if (datiComplessivi.UtenzeIdriche.Count > 0)
                        {
                            // Inserimento nuove utenze
                            foreach (var utenza in datiComplessivi.UtenzeIdriche)
                            {
                                _context.UtenzeIdriche.Add(utenza);
                            }

                            // Inserimento nuovi toponimi
                            foreach (var top in datiComplessivi.Toponimi)
                            {
                                _context.Toponomi.Add(top);
                            }

                            // Aggiornamento toponimi esistenti
                            foreach (var top in datiComplessivi.ToponimiDaAggiornare)
                            {
                                _context.Toponomi.Update(top);
                            }
                        }
                        else
                        {
                            ViewBag.Message = "Nessun dato valido trovato nel file CSV.";
                            return View("LoadFilePiranha", "Azioni");
                        }

                        // Salvataggio iniziale
                        if (datiComplessivi.UtenzeIdriche.Count > 0 || datiComplessivi.UtenzeIdricheEsistente.Count > 0)
                        {
                            await _context.SaveChangesAsync();
                        }

                        transaction.Commit();  // Confermo la transizione
                    }
                    catch (Exception dbEx)
                    {
                        transaction.Rollback();
                        _logger.LogError(dbEx, "Errore durante il salvataggio dei dati nel database.");
                        ViewBag.Message = $"Errore durante il salvataggio dei dati nel database: {dbEx.Message}";
                        return View("LoadFilePiranha", "Azioni");
                    }
                }

                // Associazione utenze senza idToponimo
               var utenzeTopNull = _context.UtenzeIdriche.Where(s => s.idToponimo == null && s.IdEnte == selectedEnteId).ToList();

                if (utenzeTopNull.Count > 0)
                {
                    // Dizionario dei toponimi già presenti, con denominazione normalizzata
                    var dizToponimi = _context.Toponomi
                        .Where(t => t.IdEnte == selectedEnteId)
                        .ToDictionary(t => FunzioniTrasversali.rimuoviVirgolette(t.denominazione ?? "").ToUpper(), t => t.id);

                    foreach (var utenza in utenzeTopNull)
                    {
                        string? denominazione = FunzioniTrasversali.rimuoviVirgolette(utenza.indirizzoUbicazione)?.ToUpper();

                        if (!string.IsNullOrEmpty(denominazione) && dizToponimi.TryGetValue(denominazione, out int? idToponimo))
                        {
                            utenza.idToponimo = idToponimo;
                            _context.UtenzeIdriche.Update(utenza);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                // Messaggio da stampare 
                ViewBag.Message = $"File '{csv_file.FileName}' caricato con successo. Utenze inserite: {datiComplessivi.UtenzeIdriche.Count}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione del file CSV.");
                ViewBag.Message = $"Errore durante l'elaborazione del file CSV: {ex.Message}";
            }
            finally
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            return View("LoadFilePiranha", "Azioni");
        }

        [HttpPost]
        public async Task<IActionResult> LoadFileINPS(IFormFile csv_file, int selectedEnteId, int serie)
        {
            // Verifico se i dati non sono null
            if (string.IsNullOrEmpty(ruolo) || string.IsNullOrEmpty(username) || idUser == 0)
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            if (csv_file == null || csv_file.Length == 0)
            {
                ViewBag.Message = "Seleziona un file CSV da caricare.";
                ViewBag.Enti = _context.Enti.ToList();
                return View("LoadFileINPS");
            }

            if (csv_file == null || csv_file.Length == 0)
            {
                ViewBag.Message = "Seleziona un file CSV da caricare.";
                return View("LoadFileINPS"); // Torna alla pagina di upload con un messaggio
            }

            // Validazione del tipo di file (opzionale ma consigliata)
            if (Path.GetExtension(csv_file.FileName).ToLowerInvariant() != ".csv")
            {
                ViewBag.Message = "Il file selezionato non è un CSV valido.";
                return View("LoadFileINPS");
            }

            var selectedEnte = await _context.Enti.FindAsync(selectedEnteId);
            if (selectedEnte == null)
            {
                ViewBag.Message = "Ente selezionato non valido.";
                ViewBag.Enti = _context.Enti.ToList();
                return View("LoadFileINPS");
            }

            string filePath = Path.GetTempFileName(); // Crea un file temporaneo

            try
            {
                // Salva il file caricato su disco
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await csv_file.CopyToAsync(stream);
                }

                // Leggi il file CSV con la tua classe CSVReader
                var datiComplessivi = CSVReader.LeggiFileINPS(filePath, _context, selectedEnteId, idUser,serie);

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Salva i report nel database
                        foreach (var report in datiComplessivi.reports)
                        {
                            _context.Reports.Add(report);
                        }

                        await _context.SaveChangesAsync();

                        transaction.Commit(); // Conferma la transazione se tutto è andato bene
                        ViewBag.Message = $"Dati caricati e salvati con successo! Reports: {datiComplessivi.reports.Count}, ";
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

            return View("LoadFileINPS"); // Torna alla pagina di upload con il messaggio di stato
        }
    }   
}
