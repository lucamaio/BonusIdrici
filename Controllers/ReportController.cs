using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BonusIdrici2.Models;
using BonusIdrici2.Data;
using System.Globalization;
using BonusIdrici2.Models.ViewModels; // Aggiungi questo using
using System.IO;

namespace BonusIdrici2.Controllers
{
    public class ReportController : Controller
    {
        private readonly ILogger<ReportController> _logger;
        private readonly ApplicationDbContext _context;

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

        // Inizio - Pagine di Navigazione

        // Pagina 1: Consente la selzione di un ente per effetuare le operazioni successive

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

            enti = _context.Enti.OrderBy(e => e.nome).ToList();
            ViewBag.Enti = enti;
            return View();
        }

        // Pagina 2: Consente la visualizzazione di tutti i report effetuati 

         [HttpPost]
        public IActionResult Show(int selectedEnteId)
        {
            if (selectedEnteId == 0)
            {
                ViewBag.Enti = _context.Enti.OrderBy(e => e.nome).ToList();
                ViewBag.Message = "Per favore, seleziona un ente valido.";
                return View("GeneraEsitoCompetenza");
            }

            // Raggruppa per utente + data di creazione
            var riepilogoDati = _context.Reports
                                        .Where(r => r.IdEnte == selectedEnteId)
                                        .GroupBy(r => new { r.IdUser, r.DataCreazione })
                                        .Select(g => new RiepilogoDatiViewModel
                                        {
                                            Iduser = g.Key.IdUser,
                                            Username = _context.Users
                                                            .Where(u => u.id == g.Key.IdUser)
                                                            .Select(u => u.Username)
                                                            .FirstOrDefault(),
                                            DataCreazione = g.Key.DataCreazione,
                                            NumeroDatiInseriti = g.Count()
                                        })
                                        .OrderByDescending(x => x.DataCreazione)
                                        .ToList();

            ViewBag.SelectedEnteId = selectedEnteId;
            ViewBag.SelectedEnteNome = _context.Enti
                                            .FirstOrDefault(e => e.id == selectedEnteId)?.nome 
                                            ?? "Ente Sconosciuto";

            return View("Show", riepilogoDati);
        }



        // Fine - Pagine di Navigazione

        // Inzio - Funzioni

        // Funzione 1: Consente il download dei file di esportazione
        
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
            var nomeEnte =ente[0].nome;

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
            }else if(tipoReport == "Siscom"){
                fileName= $"Esportazione Bonus Idrici {nomeEnte} del {timeStamp:yyyyMMddHHmmss}.csv";
                fileBytes = null;
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

        // Fine - Funzioni
    }
}