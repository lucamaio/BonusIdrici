using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BonusIdrici2.Models;
using BonusIdrici2.Data;
using System.Globalization;
using BonusIdrici2.Models.ViewModels; // Aggiungi questo using
using System.IO;
using System.Collections.Generic; 

namespace BonusIdrici2.Controllers
{
    public class ReportController : Controller
    {
        private readonly ILogger<ReportController> _logger;
        private readonly ApplicationDbContext _context;
        // private FileLog logFile = new FileLog($"wwwroot/log/Report.log");
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

        // Pagina 2: Consente la visualizzazione di tutti i report effetuati 

        [HttpPost]
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
                return View("Index", "Report");
            }

           // ✅ Raggruppa per utente + data di creazione e ordina in senso inverso

            ViewBag.Report = _context.Reports
                .Where(r => r.IdEnte == selectedEnteId)
                .GroupBy(r => r.DataCreazione)
                .Select(g => new RiepilogoDatiViewModel
                {
                    DataCreazione = g.Key,
                    idAto = g.FirstOrDefault().idAto,
                    count = g.Count(),
                    annoValidita = g.FirstOrDefault().annoValidita,
                    Username = _context.Users
                                .Where(u => u.id == g.FirstOrDefault().IdUser)
                                .Select(u => u.Username)
                                .FirstOrDefault()
                })
                .OrderByDescending(x => x.DataCreazione)
                .ToList();
            ViewBag.SelectedEnteId = selectedEnteId;
            ViewBag.SelectedEnteNome = _context.Enti.FirstOrDefault(e => e.id == selectedEnteId)?.nome?? "Ente Sconosciuto";

            return View();
        }

        // Pagina 3: Consente la visualizzazione dei dati associati a un Report

        public IActionResult Dettails(int selectedEnteId, DateTime? data, string? idAto = null)
        {
            //logFile.LogInfo($"Sono dentro la pagina Dettails. IdEnte: {selectedEnteId} | Data: {data} | idAto: {idAto}");

            // ✅ Verifica sessione
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // ✅ Validazione ente
            if (selectedEnteId == 0)
            {
                ViewBag.Enti = _context.Enti.OrderBy(e => e.nome).ToList();
                ViewBag.Message = "Per favore, seleziona un ente valido.";
                return RedirectToAction("Index", "Report");
            }

            // Lista per i dati di output
            List<RiepilogoDatiViewModel> riepilogoDati = new List<RiepilogoDatiViewModel>();

            // ✅ Caso: né data né idAto specificati
            if (data == null && idAto == null)
            {
                ViewBag.Report = _context.Reports
                    .OrderByDescending(g => g.idFornitura)
                    .ThenBy(g => g.id)
                    .Select(g => new RiepilogoDatiViewModel
                    {
                        id = g.id,
                        idAto = g.idAto,
                        codiceBonus = g.codiceBonus,
                        esitoStr = g.esitoStr,
                        esito = g.esito,
                        idFornitura = g.idFornitura,
                        codiceFiscale = g.codiceFiscaleRichiedente,
                        numeroComponenti = g.numeroComponenti,
                        serie = g.serie,
                        mc = g.mc,
                        incongruenze = g.incongruenze,
                        DataCreazione = g.DataCreazione,
                        Iduser = g.IdUser,
                        Username = _context.Users
                            .Where(u => u.id == g.IdUser)
                            .Select(u => u.Username)
                            .FirstOrDefault(),
                        NumeroDatiInseriti = 1
                    })
                    .ToList();
                
                ViewBag.Message = "Data e idAto mancanti o non validi";
                return RedirectToAction("Index", "Report");
            }

            // ✅ Caso: ricerca per data
            if (data != null)
            {
                riepilogoDati = _context.Reports
                    .Where(r => r.IdEnte == selectedEnteId && r.DataCreazione == data)
                    .OrderByDescending(r => r.idFornitura)
                    .ThenBy(r => r.id)
                    .Select(r => new RiepilogoDatiViewModel
                    {
                        id = r.id,
                        idAto = r.idAto,
                        codiceBonus = r.codiceBonus,
                        esitoStr = r.esitoStr,
                        esito = r.esito,
                        idFornitura = r.idFornitura,
                        codiceFiscale = r.codiceFiscaleRichiedente,
                        numeroComponenti = r.numeroComponenti,
                        serie = r.serie,
                        mc = r.mc,
                        incongruenze = r.incongruenze,
                        DataCreazione = r.DataCreazione,
                        Iduser = r.IdUser,
                        Username = _context.Users
                            .Where(u => u.id == r.IdUser)
                            .Select(u => u.Username)
                            .FirstOrDefault(),
                    })
                    .ToList();
            }
            // ✅ Caso: ricerca per idAto
            else if (!string.IsNullOrEmpty(idAto))
            {
                riepilogoDati = _context.Reports
                    .Where(r => r.IdEnte == selectedEnteId && r.idAto == idAto)
                    .OrderByDescending(r => r.idFornitura)
                    .ThenBy(r => r.id)
                    .Select(r => new RiepilogoDatiViewModel
                    {
                        id = r.id,
                        idAto = r.idAto,
                        codiceBonus = r.codiceBonus,
                        esitoStr = r.esitoStr,
                        esito = r.esito,
                        idFornitura = r.idFornitura,
                        codiceFiscale = r.codiceFiscaleRichiedente,
                        numeroComponenti = r.numeroComponenti,
                        serie = r.serie,
                        mc = r.mc,
                        incongruenze = r.incongruenze,
                        DataCreazione = r.DataCreazione,
                        Iduser = r.IdUser,
                        Username = _context.Users
                            .Where(u => u.id == r.IdUser)
                            .Select(u => u.Username)
                            .FirstOrDefault(),
                    })
                    .ToList();
            }
            else
            {
                return RedirectToAction("Show", "Report", new { id = selectedEnteId });
            }

            ViewBag.TotaleDomande = riepilogoDati.Count;
            ViewBag.TotaleAccettate = riepilogoDati.Count(r => r.esito == "01" && r.esitoStr == "Si");
            ViewBag.TotaleRifiutate = riepilogoDati.Count(r => r.esitoStr == "No");
            ViewBag.TotaleEsito2 = riepilogoDati.Count(r => r.esito == "02" && r.esitoStr == "Si");
            ViewBag.TotaleEsito3 = riepilogoDati.Count(r => r.esito == "03" && r.esitoStr == "Si");
            ViewBag.TotaleEsito4 = riepilogoDati.Count(r => r.esito == "04");
            ViewBag.IncongruenzeTrovate = _context.Reports.Count(r => r.IdEnte == selectedEnteId && r.DataCreazione == data && r.incongruenze == true);

            // ✅ ViewBag info
            ViewBag.SelectedEnteId = selectedEnteId;
            ViewBag.Data = data;
            ViewBag.SelectedEnteNome = _context.Enti.FirstOrDefault(e => e.id == selectedEnteId)?.nome ?? "Ente Sconosciuto";

            //logFile.LogInfo($"Dati recuperati: EnteId={ViewBag.SelectedEnteId}, Data={ViewBag.Data}, EnteNome={ViewBag.SelectedEnteNome}");

            return View("Dettails", riepilogoDati);
        }


        // Pagina 4: Consente di variare la serie per un insieme di Report

        public IActionResult VariaSerie(int? idEnte, string idAto)
        {
            //logFile.LogInfo($"Sono dentro la pagina VariaSerie.");
            // 1) Verifico la sessione

            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            //logFile.LogInfo($"IdEnte: {idEnte}, IdAto: {idAto}");

            // 2) Verifico se i dati passati sono null

            if (idEnte == null || idAto == null)
            {
                // logFile.LogError("Dati mancanti!");
                ViewBag.Message = "Dati Mancanti!";
                return RedirectToAction("Show", "Report");
            }

            // 3) Cerco il valore del campo serie sul DB

            var report = _context.Reports.FirstOrDefault(s => s.IdEnte == idEnte && s.idAto == idAto);

            // 4) Verifico se trovo un occorenza nel DB
            if (report == null)
            {
                return RedirectToAction("Show", "Report");
            }

            // 5) Creo il modello 

            var model = new RiepilogoDatiViewModel
            {
                IdEnte = idEnte,
                idAto = idAto,
                DataCreazione = report.DataCreazione,
                serie = report.serie
            };
            
            // 6) Apro la pagina
            return View(model);
        }

        // Pagina 5: Consente di Variare i dati di un Report

        public IActionResult Varia(int? id)
        {
            // 1) Verifico se esiste una sessione
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // 2) Verifico se l'id == null

            if (id == null || id == 0)
            {
                ViewBag.Message = "Id Mancante, per effetuare la variazione";
                return RedirectToAction("Show", "Report");
            }

            // 3) Mi ricavo il report dal id

            var report = _context.Reports.FirstOrDefault(s => s.id == id);

            // 4) Verifico che report non sia null
            if (report == null)
            {
                return RedirectToAction("Show", "Report");
            }

            ViewBag.Report = report;
            return View();
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
            var nomeEnte = ente[0].nome;
            List<Report> datiDelReport = new List<Report>();

            // 2. Recupero dei dati dal database (una sola query per entrambi i tipi di report)
            if (tipoReport != "Siscom")
            {
                datiDelReport = _context.Reports.Where(r => r.IdEnte == enteId && r.DataCreazione == dataCreazioneParsed).ToList();
            }
            else
            {
                // datiDelReport = _context.Reports.Where(r => r.IdEnte == enteId && r.DataCreazione == dataCreazioneParsed && (r.esito =="01" || r.esito =="02") && r.esitoStr=="Si").ToList();
                datiDelReport = _context.Reports.Where(r => r.IdEnte == enteId && r.DataCreazione == dataCreazioneParsed && r.esito == "01" && r.esitoStr == "Si").ToList();
            }

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
                fileBytes = CsvGenerator.GeneraCsvBonusIdrico(datiDelReport); // Chiamata alla funzione specifica
            }
            else if (tipoReport == "Esito Competenza Territoriale")
            {
                fileName = $"{p_iva}_BID_{dataCreazioneParsed:yyyyMM}_EBI_{timeStamp:yyyyMMddHHmmss}_{pogressivo}.csv";
                fileBytes = CsvGenerator.GeneraCsvCompetenzaTerritoriale(datiDelReport); // Chiamata alla funzione specifica
            }
            else if (tipoReport == "Siscom")
            {
                fileName = $"Esportazione Bonus Idrici {nomeEnte} del {timeStamp:yyyyMMddHHmmss}.csv";
                fileBytes = CsvGenerator.GeneraCsvSiscom(datiDelReport);
            }
            else if (tipoReport == "Debug")
            {
                fileName = $"Debug Report {nomeEnte} del {timeStamp:yyyyMMddHHmmss}.csv";
                fileBytes = CsvGenerator.GeneraCsvDebug(datiDelReport);
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

        // Funzione 2: Consente l'aggiornamento del valore di serie dei report

        [HttpPost]
        public IActionResult UpdateSerie(int idEnte, string idAto, int serie)
        {
            // logFile.LogInfo($"Sono dentro la funzione UpdateSerie.");

            // 1. Verfica la sessione
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // logFile.LogInfo($"Dati ricevuti: Id Ente: {idEnte} | idAto {idAto} | Serie {serie}");

            // 2. Mi ricavo i report presenti nel DB
            var ReportTrovati = _context.Reports.Where(s => s.IdEnte == idEnte && s.idAto == idAto);

            // 3. Verifico se non sono stati trovati dati nel DB

            if (ReportTrovati == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // 4. Leggo tutti i report da Aggiornare

            foreach (var report in ReportTrovati)
            {
                //  5) Aggiorno le proprietà
                report.serie = serie;
                report.DataAggiornamento = DateTime.Now;
            }

            // 6) Salvo i cambiamenti
            _context.SaveChanges();
            AccountController.logFile.LogInfo($"L'utente {username} ha effetuato una variazione del numero di serie per l'elaborazione INPS con idAto: {idAto} per l'ente con id {idEnte}");
            
            // 7) Torno alla pagina principale
            // return RedirectToAction("Dettails", "Report", new { selectedEnteId = idEnte, data = dataCreazione });
            return RedirectToAction("Show", "Report", new { selectedEnteId = idEnte });
        }

        // Funzione 3: Consente l'aggiornamento dei dati di un report
        [HttpPost]
        public IActionResult Update(int id, string codiceFiscale, string cognome, string nome, string esitoStr, string esito )
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
                ViewBag.Message = "Id non valido";
                return RedirectToAction("Index", "Report");
            }

            // 3) Mi ricavo il report da aggiornare

            var report = _context.Reports.FirstOrDefault(s => s.id == id);

            // 4) verifico che il report non è presente nel db
            if (report == null)
            {
                ViewBag.Message = "Report non trovato nel db";
                return RedirectToAction("Index", "Report");
            }

            AccountController.logFile.LogInfo($"L'utente {username} ha effetuato una variazione della domanda di bonus con id {report.id} è codice bonus {report.codiceBonus}");
            AccountController.logFile.LogInfo($"Prima: {report.ToString()}");
            // 5) Aggiorno i vari campi

            // report.idFornitura = idFornitura;
            report.codiceFiscaleRichiedente = codiceFiscale;
            report.cognomeDichiarante = cognome;
            report.nomeDichiarante = nome;
            report.esitoStr = esitoStr;
            report.esito = esito;
            report.DataAggiornamento = DateTime.Now;

            // 6) Salvo le modifiche sul db
            _context.SaveChanges();
            AccountController.logFile.LogInfo($"Dopo: {report.ToString()}");

            // 7) Ritorno alla pagina details
            return Dettails(id, report.DataCreazione);
        }

        // Fine - Funzioni
    }
}