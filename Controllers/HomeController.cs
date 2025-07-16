using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;
using System.IO;
using Dichiarante;
using Atto;
// using Ente;
using leggiCSV; 

namespace BonusIdrici2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context; // Assegna il DbContext iniettato
           // Console.WriteLine(_context);
        }

        public IActionResult Index()
        {
            return View();
        }

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
            List<DateTime?> date=CSVReader.LeggiDateCSV(filePath);
            Console.WriteLine(date.Count);
            if (date.Count <= 1)
            {
                 ViewBag.Message = "Data inizio e fine non trovate.";
                ViewBag.Enti = _context.Enti.ToList();
                return View("LoadFileINPS");
            }

            var fileUploadRecord = new FileUpload
            {
                NomeFile = csv_file.FileName,
                PercorsoFile = relativePath,
                DataInizio=date[0],
                DataFine=date[1],
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

                    _context.FileUploads.Add(fileUploadRecord);
                    await _context.SaveChangesAsync();

                    // var datiComplessivi = CSVReader.LeggiFileCSV(filePath);

                    // if (datiComplessivi.Dichiaranti.Any())
                    // {
                    //     foreach (var dichiarante in datiComplessivi.Dichiaranti)
                    //     {
                    //         // Aggiungi qui l'assegnazione di IdEnte al Dichiarante se non lo gestisci nel CSVReader
                    //         // dichiarante.IdEnte = selectedEnteId;
                    //         _context.Dichiaranti.Add(dichiarante);
                    //     }
                    //     await _context.SaveChangesAsync();
                    // }
                    // else
                    // {
                    //     Console.WriteLine("Nessun dichiarante trovato nel file CSV.");
                    // }

                    // // Rimossa l'assegnazione dello stato a "Completato" e il successivo SaveChangesAsync

                    transaction.Commit();

                    ViewBag.Message = $"File '{csv_file.FileName}' caricato e dati elaborati con successo per l'ente {selectedEnte.nome}. ";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Errore durante l'elaborazione o il salvataggio del file CSV.");

                    // Rimossa la logica per aggiornare Stato e NoteErrore nel database

                    ViewBag.Message = $"Errore durante l'elaborazione del file CSV: {ex.Message}";
                }
            }

            ViewBag.Enti = _context.Enti.ToList();
            return View("LoadFileINPS");
        }
         public IActionResult Login()
        {
            return View();
        }

         public IActionResult InsertEnte()
        {
            return View();
        }

          public IActionResult Privacy()
        {
            return View();
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
                    return RedirectToAction("Index"); // oppure una vista di conferma
                }
            // In caso di errore, ritorna la stessa vista con il modello
            return View("InsertEnte");
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}