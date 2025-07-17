using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;
using System.IO;
using Dichiarante;
using Atto;
using leggiCSV;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BonusIdrici2.Controllers
{
    public class AzioniController : Controller
    {
        private readonly ILogger<AzioniController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext
        public AzioniController(ILogger<AzioniController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context; // Assegna il DbContext iniettato
                                // Console.WriteLine(_context);
        }

        // Pagine di navigazione
        public IActionResult LoadAnagrafica()
        {
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
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoadFileINPSAction(IFormFile csv_file, int selectedEnteId)
        {
            if (csv_file == null || csv_file.Length == 0)
            {
                ViewBag.Message = "Seleziona un file CSV da caricare.";
                ViewBag.Enti = _context.Enti.ToList();
                return View("LoadFileINPS");
            }

            if (Path.GetExtension(csv_file.FileName).ToLowerInvariant() != ".csv")
            {
                ViewBag.Message = "Il file selezionato non è un CSV valido.";
                ViewBag.Enti = _context.Enti.ToList();
                return View("LoadFileINPS");
            }

            var selectedEnte = await _context.Enti.FindAsync(selectedEnteId);
            if (selectedEnte == null)
            {
                ViewBag.Message = "Ente selezionato non valido.";
                ViewBag.Enti = _context.Enti.ToList();
                return View("LoadFileINPS");
            }

            string uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads", selectedEnte.nome);
            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            string filePath = Path.Combine(uploadDirectory, csv_file.FileName);
            string relativePath = Path.Combine("Uploads", selectedEnte.nome, csv_file.FileName);
            var fileUploadRecord = new FileUpload
            {
                NomeFile = csv_file.FileName,
                PercorsoFile = relativePath,
                DataCaricamento = DateTime.Now,
                IdEnte = selectedEnteId
            };

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await csv_file.CopyToAsync(stream);
                    }

                    List<DateTime?> date = CSVReader.LeggiDateCSV(filePath);

                    if (date.Count < 2)
                    {
                        ViewBag.Message = "Data inizio e fine non trovate.";
                        ViewBag.Enti = _context.Enti.ToList();
                        return View("LoadFileINPS");
                    }

                    fileUploadRecord.DataInizio = date[0];
                    fileUploadRecord.DataFine = date[1];

                    _context.FileUploads.Add(fileUploadRecord);
                    await _context.SaveChangesAsync();

                    transaction.Commit();

                    ViewBag.Message = $"File '{csv_file.FileName}' caricato e dati elaborati con successo per l'ente {selectedEnte.nome}.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Errore durante l'elaborazione o il salvataggio del file CSV.");

                    ViewBag.Message = $"Errore durante l'elaborazione del file CSV: {ex.Message}";
                }
            }


            ViewBag.Enti = _context.Enti.ToList();
            return View("LoadFileINPS");
        }
      

        [HttpPost]
        public async Task<IActionResult> UploadCsv(IFormFile csv_file) // Il nome del parametro deve corrispondere al 'name' dell'input file nel form
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

            string filePath = Path.GetTempFileName(); // Crea un file temporaneo

            try
            {
                // Salva il file caricato su disco
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await csv_file.CopyToAsync(stream);
                }

                // Leggi il file CSV con la tua classe CSVReader
                var datiComplessivi = CSVReader.LeggiFileCSV(filePath);

                // Inizia una transazione per assicurare che tutti i dati vengano salvati
                // o nessuno in caso di errore. (Opzionale ma buona pratica per operazioni multiple)
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Salva i dichiaranti nel database
                        foreach (var dichiarante in datiComplessivi.Dichiaranti)
                        {
                            _context.Dichiaranti.Add(dichiarante);
                        }
                        await _context.SaveChangesAsync(); // Salva i dichiaranti prima

                        // Salva gli atti nel database
                        // foreach (var atto in datiComplessivi.Atti)
                        // {
                        //     _context.Atti.Add(atto);
                        // }
                        // await _context.SaveChangesAsync(); // Salva gli atti

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
        public IActionResult creaEnte(Ente ente)
        {
            if (ModelState.IsValid)
            {
                //  string nome = ente.Nome;
                // string istat = ente.Istat;
                // string codiceFiscale = ente.CodiceFiscale;
                // string provincia = ente.Provincia;
                // string regione = ente.Regione;
                // Logica per salvare l'ente nel database

                _context.Enti.Add(ente);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Ente creato con successo.";
                return RedirectToAction("Index","Home"); // oppure una vista di conferma
            }
            // In caso di errore, ritorna la stessa vista con il modello
            return View("InsertEnte");
        }


    }
    
}