using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Models;
using Data;
using System.Globalization;
using Models.ViewModels; // Aggiungi questo using
using System.IO;
using System.Collections.Generic; 

namespace Controllers
{
    public class ReportController : Controller
    {
        private readonly ILogger<ReportController> _logger;
        private readonly ApplicationDbContext _context;
        private FileLog logFile = new FileLog($"wwwroot/log/Domande.log");
        private string? ruolo;
        private int idUser;
        private string? username;

        public ReportController(ILogger<ReportController> logger, ApplicationDbContext context)
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

        // Pagina 1: Consente la selzione di un ente per effetuare le operazioni successive

        public IActionResult Index()
        {
            // 1) Verifico che esista una sessione
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // 2) Mi ricavo gli enti
            List<Ente> enti = new List<Ente>();

            if (ruolo == "OPERATORE")
            {
                // 2.a) Verifico se gestisce un solo ente
                enti = FunzioniTrasversali.GetEnti(_context, idUser);
                if (enti.Count == 1)
                {
                    return Show(enti[0].id);
                }

                // 2.b)  Altrimenti mostro so gli enti su cui l'utente opera

                ViewBag.Enti = enti;
                return View();
            }
            // 2.c) Se sono amministratore me li mostri tutti
            enti = _context.Enti.OrderBy(e => e.nome).ToList();
            ViewBag.Enti = enti;
            return View();
        }


        // Pagina 2: Consente la visualizzazione di tutti i domande effetuati 

        [HttpGet, HttpPost]
        public IActionResult Show(int selectedEnteId)
        {
            // Verifica la sessione
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            if (selectedEnteId == 0)
            {
                ViewBag.Enti = _context.Enti.OrderBy(e => e.nome).ToList();
                ViewBag.Message = "Per favore, seleziona un ente valido.";
                return View("Index", "Domande");
            }

            // Mi recupuero tutti i report relativi all'ente

            var reportEffettuati = _context.Reports
                .Where(r => r.idEnte == selectedEnteId)
                .GroupBy(r => new { r.id, r.idEnte, r.idUser, r.serie, r.DataCreazione, r.stato, r.mese, r.anno })
                .Select(g => new Models.ViewModels.ReportViewModel
                {
                    id = g.Key.id,
                    idEnte = g.Key.idEnte,
                    DataCreazione = g.Key.DataCreazione,
                    mese = g.Key.mese,
                    anno = g.Key.anno,
                    stato = g.Key.stato,
                    serie = g.Key.serie,
                    count = _context.Domande.Where(d => d.idReport == g.Key.id).Count(),
                    Username = _context.Users
                        .Where(u => u.id == g.Key.idUser)
                        .Select(u => u.Username)
                        .FirstOrDefault()
                })
                .OrderByDescending(r => r.DataCreazione)
                .ToList();

            ViewBag.Reports = reportEffettuati ?? null;
            ViewBag.idEnte = selectedEnteId;
            ViewBag.SelectedEnteNome = _context.Enti.Where(e => e.id == selectedEnteId).Select(e => e.nome).FirstOrDefault();
            return View();
        }

        // Pagina 3: Consente la visualizzazione dei dati associati a un Domande
        [HttpGet, HttpPost]
        public IActionResult Dettails(int idReport)
        {
            // 1. Verifico se esiste una sessione attiva

            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // 2. Verifico che l'id del report non è 0

            if (idReport == 0)
            {
                ViewBag.Message = "Id Report non valido, riporova";
                return RedirectToAction("Index", "Home");
            }

            // 3. Verifico che l'id esiste nel db

            var reportEsistente = _context.Reports.FirstOrDefault(r => r.id == idReport);
            if (reportEsistente == null)
            {
                ViewBag.Message = "ID report non trovato! Riprova";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.idReport = idReport;
            ViewBag.idEnte = reportEsistente.idEnte;

            // 4. Mi ricavo tutte le domande associate al report

            var domandeReport = _context.Domande.Where(d => d.idReport == idReport).ToList();
            ViewBag.Domande = domandeReport;

            // 5. Salvo la data di elaborazione e il nome dal ente in modo da poterli visualizzare
            var elaborazione = reportEsistente.mese + " " + reportEsistente.anno;
            ViewBag.ElaborazioneDel = elaborazione;
            ViewBag.nomeEnte = _context.Enti.Where(e => e.id == reportEsistente.idEnte).Select(e => e.nome).FirstOrDefault();

            // 6. Mi ricavo le informazioni aggiuntive per la parte relativa alle statistiche

            ViewBag.TotaleDomande = domandeReport.Count();
            ViewBag.DomandeValide = domandeReport.Count(d => d.esito == "01" && d.esitoStr == "Si");
            ViewBag.TotaleRifiutate = domandeReport.Count(d => d.esitoStr == "No");

            ViewBag.DomandeEsito1 = domandeReport.Count(d => d.esito == "01");
            ViewBag.DomandeEsito2 = domandeReport.Count(d => d.esito == "02");
            ViewBag.DomandeEsito3 = domandeReport.Count(d => d.esito == "03");
            ViewBag.DomandeEsito4 = domandeReport.Count(d => d.esito == "04");

            ViewBag.incongruenze = domandeReport.Count(d => d.incongruenze == true);
            
            return View();
        }

        // Pagina 4: Consente di variare la serie per un insieme di Domande

        public IActionResult VariaSerie(int idReport)
        {
            // 1. Verifico se esiste una sessione attiva
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // 2. Verifico che l'id del report non è 0

            if (idReport == 0)
            {
                ViewBag.Message = "Id Report non valido, riporova";
                return RedirectToAction("Index", "Home");
            }

            // 3. Verifico che l'id esiste nel db

            var reportEsistente = _context.Reports.FirstOrDefault(r => r.id == idReport);
            if (reportEsistente == null)
            {
                ViewBag.Message = "ID report non trovato! Riprova";
                return RedirectToAction("Index", "Home");
            }

            // 4. Salvo il valore della serie e mostro la pagina
            ViewBag.idReport = idReport;
            ViewBag.idEnte = reportEsistente.idEnte; // Invio l'id del ente in modo di poter ritorare alla pagina precedente
            ViewBag.Serie = reportEsistente.serie;
            return View();
        }

        // Pagina 5: Consente di Variare i dati di un Domande
        [HttpGet, HttpPost]
        public IActionResult Varia(int id)
        {
            // 1) Verifico se esiste una sessione
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // 2) Verifico se l'id e maggiore di zero

            if (id <= 0) {
                ViewBag.Message = "ID non valido! Riprova";
                return RedirectToAction("Index", "Home");
            }

            // 3) Verifico che la domanda esista

            var domandaEsistente = _context.Domande.FirstOrDefault(d => d.id == id);
            if (domandaEsistente == null)
            {
                ViewBag.Message = "Domanda non trovata riprova";
                return RedirectToAction("Index", "Home");
            }

            // 4) Invio i dati e visualizzo la pagina
            ViewBag.Domanda = domandaEsistente;
            ViewBag.Serie = _context.Reports.Where(r => r.id == domandaEsistente.idReport).Select(r => r.serie).FirstOrDefault();
            return View();
        }

        // Fine - Pagine di Navigazione

        // Inzio - Funzioni

        // Funzione 1: Consente il download dei file di esportazione

        public async Task<IActionResult> ScaricaCsv(int idReport, string tipoEsportazione)
        {
            // 1) Validazione input
            if (idReport <= 0 || string.IsNullOrEmpty(tipoEsportazione))
            {
                return BadRequest("Parametri mancanti per il download del domande.");
            }
            // 2) Verifico che esista il report

            var reportEsistente = _context.Reports.FirstOrDefault(r => r.id == idReport);
            if (reportEsistente == null)
            {
                return BadRequest("Report non trovato!");
            }

            var serie = reportEsistente.serie; // Variabile neccessaria solo nel caso in cui il tipo di esportazione è siscom o Debug

            // 3) Mi ricavo i dati relativi all'ente

            var ente = _context.Enti.FirstOrDefault(e => e.id == reportEsistente.idEnte);
            if (ente == null)
            {
                return BadRequest("Ente non trovato!");
            }

            var p_iva = ente.partitaIva;
            var nomeEnte = ente.nome;

            // 4) Addesso recupero i dati dal DB
            List<Domanda> domande = new List<Domanda>();

            if (tipoEsportazione != "Siscom")
            {
                domande = _context.Domande.Where(r => r.idReport == idReport).ToList();
            }
            else
            {
                domande = _context.Domande.Where(r => r.idReport == idReport && r.esito == "01" && r.esitoStr == "Si").ToList();
            }

            // 5) Definizione delle variabili neccessarie a generare il file
            byte[]? fileBytes;
            string fileName = "";
            string contentType = "text/csv";
            DateTime timeStamp = DateTime.Now;
            var pogressivo = "1";

            // 6) Chiama la funzione di generazione CSV appropriata in base al tipo di domande
            if (tipoEsportazione == "Esito Bonus Idrico")
            {
                // <PIVA_Utente>_BID_<AAAAMM>_EBI_<timestamp>_<progressivo>.csv
                fileName = $"{p_iva}_BID_{reportEsistente.DataCreazione:yyyyMM}_EBI_{timeStamp:yyyyMMddHHmmss}_{pogressivo}.csv";
                fileBytes = CsvGenerator.GeneraCsvBonusIdrico(domande); // Chiamata alla funzione specifica
            }
            else if (tipoEsportazione == "Esito Competenza Territoriale")
            {
                fileName = $"{p_iva}_BID_{reportEsistente.DataCreazione:yyyyMM}_EBI_{timeStamp:yyyyMMddHHmmss}_{pogressivo}.csv";
                fileBytes = CsvGenerator.GeneraCsvCompetenzaTerritoriale(domande); // Chiamata alla funzione specifica
            }
            else if (tipoEsportazione == "Siscom")
            {
                fileName = $"Esportazione Bonus Idrici {nomeEnte} del {timeStamp:yyyyMMddHHmmss}.csv";
                fileBytes = CsvGenerator.GeneraCsvSiscom(domande,serie);
            }
            else if (tipoEsportazione == "Debug")
            {
                fileName = $"Debug Domande {nomeEnte} del {timeStamp:yyyyMMddHHmmss}.csv";
                fileBytes = CsvGenerator.GeneraCsvDebug(domande,serie);
            }
            else
            {
                return NotFound("Tipo di domande non riconosciuto o non supportato per la generazione CSV.");
            }

            // 7) Imposta l'header Content-Disposition
            var contentDisposition = new System.Net.Mime.ContentDisposition
            {
                FileName = fileName,
                Inline = false,
            };
            Response.Headers.Append("Content-Disposition", contentDisposition.ToString());

            // 8) Restituisci i byte come file
            return File(fileBytes, contentType, fileName);
        }

        // Funzione 2: Consente l'aggiornamento del valore di serie dei domande

        [HttpPost]
        public IActionResult UpdateSerie(int idReport, int serie)
        {

            // 1. Verfica la sessione
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // 2. Verifico se idReport è 0 e se serie e valido

            if (idReport == 0 || serie < 0)
            {
                ViewBag.Message = "Id report o serie non è valido! Riprova";
                return RedirectToAction("Index", "Report");
            }

            // 3. Verifico l'esistenza del report

            var reportEsistente = _context.Reports.FirstOrDefault(r => r.id == idReport);
            if (reportEsistente == null)
            {
                ViewBag.Message = "Report non trovato! Riprova";
                return RedirectToAction("Index", "Report");
            }

            // 4. Aggiorno il report

            reportEsistente.serie = serie;
            reportEsistente.DataAggiornamento = DateTime.Now;

            // 5) Salvo i cambiamenti
            _context.SaveChanges();

            // 6) Torno alla pagina principale
            return RedirectToAction("Show", "Report", new { selectedEnteId = reportEsistente.idEnte });
        }

        // Funzione 3: Consente l'aggiornamento dati relativi ad una domanda
        [HttpPost]
        public IActionResult Update(int id, string codiceFiscaleRichiedente, string cognome, string nome, string esitoStr, string esito)
        {
            // 1) Verifico se esiste una sessione attiva
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // 2) Verifico che id non sia <= 0
            if (id <= 0)
            {
                ViewBag.Message = "Id non valido!";
                return RedirectToAction("Index", "Report");
            }

            // 3) Mi ricavo il domande da aggiornare

            var domandaEsistente = _context.Domande.FirstOrDefault(s => s.id == id);

            // 4) verifico che il domande non è presente nel db
            if (domandaEsistente == null)
            {
                ViewBag.Message = "Domande non trovato nel db";
                return RedirectToAction("Index", "Report");
            }

            //AccountController.logFile.LogInfo($"L'utente {username} ha effetuato una variazione della domanda di bonus con id {domande.id} è codice bonus {domande.codiceBonus}");
            //AccountController.logFile.LogInfo($"Prima: {domande.ToString()}");
            // 5) Aggiorno i vari campi

            if (codiceFiscaleRichiedente != domandaEsistente.codiceFiscaleRichiedente)
            {
                domandaEsistente.codiceFiscaleRichiedente = codiceFiscaleRichiedente;
            }
            domandaEsistente.cognomeDichiarante = cognome;
            domandaEsistente.nomeDichiarante = nome;
            domandaEsistente.esitoStr = esitoStr;
            domandaEsistente.esito = esito;
            domandaEsistente.DataAggiornamento = DateTime.Now;

            // 6) Salvo le modifiche sul db
            _context.SaveChanges();
            //AccountController.logFile.LogInfo($"Dopo: {domandeEsistente.ToString()}");

            // 7) Ritorno alla pagina details
            // return Dettails(domandeEsistente.idReport);
            return RedirectToAction("Dettails", "Report", new { idReport = domandaEsistente.idReport });
        }

        // Fine - Funzioni
    }
}