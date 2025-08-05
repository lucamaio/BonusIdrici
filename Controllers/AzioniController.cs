using Microsoft.AspNetCore.Mvc;
using BonusIdrici2.Models;
using BonusIdrici2.Data;
using System.Globalization;
using BonusIdrici2.Models.ViewModels; // Aggiungi questo using

namespace BonusIdrici2.Controllers
{
    public class AzioniController : Controller
    {
        private readonly ILogger<AzioniController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext
        public AzioniController(ILogger<AzioniController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Pagine di navigazione
        public IActionResult LoadAnagrafica()
        {
            // Recupera tutti gli enti dal database
            List<Ente> enti = _context.Enti.ToList();
            // Passa la lista degli enti alla vista tramite ViewBag
            ViewBag.Enti = enti;
            return View();
        }

        public IActionResult LoadFileINPS()
        {
            // Recupera tutti gli enti dal database
            List<Ente> enti = _context.Enti.ToList();
            // Passa la lista degli enti alla vista tramite ViewBag
            ViewBag.Enti = enti;
            return View(); // Assicurati che il nome della vista sia corretto (es. LoadFileINPS.cshtml)
        }

        public IActionResult InsertEnte()
        {
            return View();
        }
        public IActionResult LoadFilePiranha()
        {
            List<Ente> enti = _context.Enti.ToList();
            // Passa la lista degli enti alla vista tramite ViewBag
            ViewBag.Enti = enti;
            return View();
        }

        public IActionResult Report()
        {
            // ViewBag.Enti = _context.Enti.OrderBy(e => e.nome).ToList();
            List<Ente> enti = _context.Enti.ToList();
            ViewBag.Enti = enti;
            return View();
        }

        public IActionResult Toponomi()
        {
            // ViewBag.Enti = _context.Enti.OrderBy(e => e.nome).ToList();
            List<Ente> enti = _context.Enti.OrderBy(e => e.nome).ToList();
            ViewBag.Enti = enti;
            return View();
        }

        public IActionResult CreateToponimo(int idEnte)
        {
            ViewBag.IdEnte = idEnte;
            return View();
        }

        public IActionResult ModificaToponimo(int id, string denominazione, string? normalizzazione, DateTime data_creazione, int idEnte){
            ViewBag.id=id;
            ViewBag.denominazione=denominazione;
            ViewBag.normalizzazione=normalizzazione;
            ViewBag.data_creazione=data_creazione;
            ViewBag.IdEnte=idEnte;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoadAnagrafe(IFormFile csv_file, int selectedEnteId) // Il nome del parametro deve corrispondere al 'name' dell'input file nel form
        {
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
                var datiComplessivi = CSVReader.LoadAnagrafe(filePath, selectedEnteId, dichiaranti);

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
            if (csv_file == null || csv_file.Length == 0)
            {
                ViewBag.Message = "Seleziona un file CSV da caricare.";
                ViewBag.Enti = _context.Enti.ToList();
                return View("LoadFilePiranha", "Azioni");
            }

            if (csv_file == null || csv_file.Length == 0)
            {
                ViewBag.Message = "Seleziona un file CSV da caricare.";
                return View("LoadFilePiranha", "Azioni"); // Torna alla pagina di upload con un messaggio
            }

            // Validazione del tipo di file (opzionale ma consigliata)
            if (Path.GetExtension(csv_file.FileName).ToLowerInvariant() != ".csv")
            {
                ViewBag.Message = "Il file selezionato non è un CSV valido.";
                return View("LoadFilePiranha", "Azioni");
            }

            var selectedEnte = await _context.Enti.FindAsync(selectedEnteId);

            if (selectedEnte == null)
            {
                ViewBag.Message = "Ente selezionato non valido.";
                ViewBag.Enti = _context.Enti.ToList();
                return View("LoadFilePiranha", "Azioni");
            }

            string filePath = Path.GetTempFileName(); // Crea un file temporaneo

            List<UtenzaIdrica> utenzeIdriche = _context.UtenzeIdriche.Where(u => u.IdEnte == selectedEnteId).ToList();
            try
            {
                // Salva il file caricato su disco
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await csv_file.CopyToAsync(stream);
                }

                // Leggi il file CSV con la tua classe CSVReader
                var datiComplessivi = CSVReader.LeggiFilePhirana(filePath, selectedEnteId, utenzeIdriche, _context);
                if (datiComplessivi != null)
                {
                    using (var transaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            if (datiComplessivi.UtenzeIdriche.Count > 0)
                            {
                                foreach (var UtenzeIdrica in datiComplessivi.UtenzeIdriche)
                                {
                                    _context.UtenzeIdriche.Add(UtenzeIdrica);
                                }
                                if (datiComplessivi.Toponimi.Count > 0)
                                {
                                    foreach (var top in datiComplessivi.Toponimi)
                                    {
                                        _context.Toponomi.Add(top);
                                    }
                                }
                                if (datiComplessivi.ToponimiDaAggiornare.Count > 0)
                                {
                                    foreach (var top in datiComplessivi.ToponimiDaAggiornare)
                                    {
                                        _context.Toponomi.Update(top);
                                    }
                                }

                            }
                            // else if(datiComplessivi.UtenzeIdricheEsistente.Count > 0)
                            // {
                            //     // Se non ci sono nuove utenze, aggiorna quelle esistenti
                            //     foreach (var utenzaEsistente in datiComplessivi.UtenzeIdricheEsistente)
                            //     {
                            //         _context.UtenzeIdriche.Update(utenzaEsistente);
                            //     }
                            // }
                            else
                            {
                                ViewBag.Message = "Nessun dato valido trovato nel file CSV.";
                                return View("LoadFilePiranha", "Azioni");
                            }

                            // Salva le modifiche al database
                            if (datiComplessivi.UtenzeIdriche.Count > 0 || datiComplessivi.UtenzeIdricheEsistente.Count > 0)
                            {
                                await _context.SaveChangesAsync();
                            }

                            transaction.Commit(); // Conferma la transazione se tutto è andato bene
                            ViewBag.Message = $"File '{csv_file.FileName}' caricato e dati salvati con successo! Utenze: {datiComplessivi.UtenzeIdriche.Count}, ";
                        }
                        catch (Exception dbEx)
                        {
                            transaction.Rollback(); // Annulla la transazione in caso di errore
                            _logger.LogError(dbEx, "Errore durante il salvataggio dei dati nel database.");
                            ViewBag.Message = $"Errore durante il salvataggio dei dati nel database: {dbEx.Message}";
                        }
                    }
                }
                else
                {
                    ViewBag.Message = "Nessun dato valido trovato nel file CSV.";
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

            return View("LoadFilePiranha", "Azioni"); // Torna alla pagina di upload con il messaggio di stato
        }


        [HttpPost]
        public async Task<IActionResult> LoadFileINPS(IFormFile csv_file, int selectedEnteId)
        {
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
                var datiComplessivi = CSVReader.LeggiFileINPS(filePath, _context, selectedEnteId);

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
        // POST: Gestisce la selezione dell'ente e reindirizza alla pagina di riepilogo
        [HttpPost]
        public IActionResult RiepilogoDatiEnte(int selectedEnteId)
        {
            if (selectedEnteId == 0) // Controlla se l'ID è valido, 0 potrebbe essere il valore di default per "-- Seleziona un Ente --"
            {
                ViewBag.Enti = _context.Enti.OrderBy(e => e.nome).ToList(); // Ricarica gli enti per la vista
                ViewBag.Message = "Per favore, seleziona un ente valido.";
                return View("GeneraEsitoCompetenza"); // Torna alla pagina di selezione
            }

            // Recupera le date di inserimento uniche e il conteggio dei report per quell'ente
            // Assumi che il tuo modello Report abbia idEnte e data_inserimento
            var riepilogoDati = _context.Reports
                                        .Where(r => r.IdEnte == selectedEnteId)
                                        .GroupBy(r => r.DataCreazione) // Raggruppa per data di inserimento
                                        .Select(g => new RiepilogoDatiViewModel
                                        {
                                            DataCreazione = g.Key,
                                            NumeroDatiInseriti = g.Count()
                                        })
                                        .OrderByDescending(x => x.DataCreazione) // Ordina dalla data più recente
                                        .ToList();

            // Passa i dati alla vista, inclusa l'ID dell'ente selezionato
            ViewBag.SelectedEnteId = selectedEnteId;
            ViewBag.SelectedEnteNome = _context.Enti.FirstOrDefault(e => e.id == selectedEnteId)?.nome ?? "Ente Sconosciuto";

            return View("RiepilogoDatiEnte", riepilogoDati);
        }

        public IActionResult ShowToponomi(int selectedEnteId)
        {
            if (selectedEnteId == 0)
            {
                ViewBag.Enti = _context.Enti.OrderBy(e => e.nome).ToList();
                ViewBag.Message = "Per favore, seleziona un ente valido.";
                return View("Toponomi");
            }

            var dati = _context.Toponomi
                        .Where(r => r.IdEnte == selectedEnteId)
                        .OrderByDescending(x => x.denominazione)
                        .ToList();

            var viewModelList = dati.Select(x => new ToponomiViewModel
            {
                id= x.id,
                denominazione = x.denominazione,
                normalizzazione = x.normalizzazione,
                data_creazione = x.data_creazione,
                data_aggiornamento = x.data_aggiornamento,
                IdEnte = x.IdEnte
            }).ToList();

            ViewBag.SelectedEnteId = selectedEnteId;
            ViewBag.SelectedEnteNome = _context.Enti.FirstOrDefault(e => e.id == selectedEnteId)?.nome ?? "Ente Sconosciuto";

            return View("ShowToponomi", viewModelList);
        }

        [HttpPost]
        public IActionResult creaToponimo(string denominazione, string normalizzazione, int idEnte)
        {
            var nuovoToponimo = new Toponimo
            {
                denominazione = denominazione.Trim().ToUpper(),
                normalizzazione = normalizzazione.Trim().ToUpper(),
                IdEnte = idEnte,
                data_creazione = DateTime.Now,
                data_aggiornamento =null
            };

            _context.Toponomi.Add(nuovoToponimo);
            _context.SaveChanges();

            return RedirectToAction("ShowToponomi", "Azioni", new { selectedEnteId = idEnte });
        }

        [HttpPost]
        public IActionResult UpdateToponimo(int id, string denominazione, string normalizzazione, DateTime data_creazione, int idEnte)
        {
            var toponimoEsistente = _context.Toponomi.FirstOrDefault(t => t.id == id);

            if (toponimoEsistente == null)
            {
                return RedirectToAction("Index","Home"); // oppure restituisci una view con errore
            }

            // Aggiorna le proprietà
            toponimoEsistente.denominazione = denominazione.Trim().ToUpper();
            toponimoEsistente.normalizzazione = normalizzazione.Trim().ToUpper();
            toponimoEsistente.data_aggiornamento = DateTime.Now;
            // data_creazione non viene modificata
            toponimoEsistente.IdEnte = idEnte;

            _context.SaveChanges();

            return RedirectToAction("ShowToponomi", "Azioni", new { selectedEnteId = idEnte });
        }


        public async Task<IActionResult> ScaricaCsv(int enteId, string DataCreazione, string tipoReport)
        {
            // 1. Validazione input
            if (string.IsNullOrEmpty(DataCreazione) || string.IsNullOrEmpty(tipoReport))
            {
                return BadRequest("Parametri mancanti per il download del report.");
            }

            DateTime dataCreazioneParsed;
            if (!DateTime.TryParseExact(DataCreazione, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dataCreazioneParsed))
            {
                return BadRequest("Formato data non valido. Usare il formato AAAA-MM-GG.");
            }

            var ente = _context.Enti.Where(r => r.id == enteId).ToList();
            var p_iva = ente[0].partitaIva;

            // 2. Recupero dei dati dal database (una sola query per entrambi i tipi di report)
            List<Report> datiDelReport = _context.Reports
                                                .Where(r => r.IdEnte == enteId && r.DataCreazione == dataCreazioneParsed)
                                                .ToList();

            byte[]? fileBytes;
            string fileName = "";
            string contentType = "text/csv";
            DateTime timeStamp = DateTime.Now;
            var pogressivo = "1";

            // 3. Chiama la funzione di generazione CSV appropriata in base al tipo di report
            if (tipoReport == "Esito Bonus Idrico")
            {
                // <PIVA_Utente>_BID_<AAAAMM>_EBI_<timestamp>_<progressivo>.csv
                fileName = $"{p_iva}_BID_{dataCreazioneParsed:yyyyMM}_EBI_{timeStamp:yyyyMMddHHmmss}_{pogressivo}.csv";
                fileBytes = null;
                fileBytes = CsvGenerator.GeneraCsvBonusIdrico(datiDelReport); // Chiamata alla funzione specifica
            }
            else if (tipoReport == "Esito Competenza Territoriale")
            {
                fileName = $"{p_iva}_BID_{dataCreazioneParsed:yyyyMM}_EBI_{timeStamp:yyyyMMddHHmmss}_{pogressivo}.csv";
                fileBytes = CsvGenerator.GeneraCsvCompetenzaTerritoriale(datiDelReport); // Chiamata alla funzione specifica
            }
            else
            {
                return NotFound("Tipo di report non riconosciuto o non supportato per la generazione CSV.");
            }

            // 4. Imposta l'header Content-Disposition
            var contentDisposition = new System.Net.Mime.ContentDisposition
            {
                FileName = fileName,
                Inline = false,
            };
            Response.Headers.Append("Content-Disposition", contentDisposition.ToString());

            // 5. Restituisci i byte come file
            return File(fileBytes, contentType, fileName);
        }
    }   
}
