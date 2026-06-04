using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Models; 
using Data;
using System.IO;
using Models.ViewModels;
using BonusIdrici2.Services;
using Microsoft.EntityFrameworkCore;

namespace Controllers
{
    public class UtenzeController : Controller
    {
        // Dichiarazione delle variabili di istanza
        private readonly ILogger<UtenzeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly SectionActivityService _sectionActivityService;
        private readonly AppCacheService _cache;

        private string? ruolo;
        private int? idUser;
        private string? username;

        // Costruttore

        public UtenzeController(ILogger<UtenzeController> logger, ApplicationDbContext context, SectionActivityService sectionActivityService, AppCacheService cache)
        {
            _logger = logger;
            _context = context;
            _sectionActivityService = sectionActivityService;
            _cache = cache;

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
                idUser = null;
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

        // Inizio - Pagine di navigazione

        // Pagina 1: Pagina Home che consente la selezione di un ente per poi visualizzarne le utenze idriche
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

        //Pagina 2: Pagina che consente la vissualizzazione di tutte le utenze del ente selezionato

        public IActionResult Show(int selectedEnteId)
        {
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            if (selectedEnteId == 0)
            {
                ViewBag.Enti = _cache.GetOrCreate(
                    "enti:all",
                    () => _context.Enti.AsNoTracking().OrderBy(e => e.nome).ToList(),
                    AppCacheService.EntiExpiration);
                ViewBag.Message = "Per favore, seleziona un ente valido.";
                return View("Index", "Utenze");
            }

            var dati = _cache.GetOrCreate(
                $"utenze:ente:{selectedEnteId}",
                () => _context.UtenzeIdriche.AsNoTracking().Where(r => r.IdEnte == selectedEnteId).ToList(),
                AppCacheService.UtenzeExpiration);

            var viewModelList = dati.Select(x => new UtenzeViewModel
            {
                id = x.id,
                idAcquedotto = x.idAcquedotto,
                stato = x.stato,
                periodoIniziale = x.periodoIniziale,
                periodoFinale = x.periodoFinale,
                matricolaContatore = x.matricolaContatore,
                indirizzoUbicazione = x.indirizzoUbicazione,
                numeroCivico = x.numeroCivico,
                subUbicazione = x.subUbicazione,
                scalaUbicazione = x.scalaUbicazione,
                piano = x.piano,
                interno = x.interno,
                tipoUtenza = x.tipoUtenza,
                cognome = x.cognome,
                nome = x.nome,
                sesso = x.sesso,
                codiceFiscale = x.codiceFiscale,
                data_creazione = x.data_creazione,
                data_aggiornamento = x.data_aggiornamento,
                IdEnte = x.IdEnte
            }).ToList();

            ViewBag.TotaleUtenze = viewModelList.Count;
            ViewBag.UtenzeIscrivendo = viewModelList.Count(s => s.stato == 1 || s.stato == 2);
            ViewBag.UtenzeCancellate = viewModelList.Count(s => s.stato == 4 || s.stato == 5);
            ViewBag.UtenzeIscrivendoCancellando = viewModelList.Count(s => s.stato == 3);
            ViewBag.UtenzeDomestiche = viewModelList.Count(s => s.tipoUtenza == "UTENZA DOMESTICA");
            ViewBag.UtenzeNonDomestiche = viewModelList.Count(s => s.tipoUtenza != "UTENZA DOMESTICA");

            ViewBag.SelectedEnteId = selectedEnteId;
            ViewBag.SelectedEnteNome = _cache.GetOrCreate(
                $"enti:detail:{selectedEnteId}",
                () => _context.Enti.AsNoTracking().FirstOrDefault(e => e.id == selectedEnteId),
                AppCacheService.EntiExpiration)?.nome ?? "Ente Sconosciuto";
            ViewBag.SectionActivity = _sectionActivityService.GetUtenzeActivity(selectedEnteId);

            return View("Show", viewModelList);
        }

        // Pagina 3: Pagina che consente l'inserimento di una nuova utenza
        public IActionResult Create(int idEnte)
        {
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.IdEnte = idEnte;
            return View();
        }

        // Pagina 4: Pagina che consente di modificare i dati di una utenza
        public IActionResult Modifica(int id)
        {
            if (!VerificaSessione())
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }
           
            ViewBag.id = id;
            var utenza = _context.UtenzeIdriche.FirstOrDefault(s => s.id == id);
            var getNominativoDichiarante = FunzioniTrasversali.getNominativoDichiarante(_context, utenza?.IdDichiarante);
            var denominazioneToponimo = utenza?.idToponimo != null ? _context.Toponomi.FirstOrDefault(s => s.id == utenza.idToponimo) : null;
            ViewBag.Utenza = utenza;
            ViewBag.nominativoDichiarante = getNominativoDichiarante;
            ViewBag.denominazioneToponimo = denominazioneToponimo != null ? denominazioneToponimo.denominazione : null;
            return View();
        }

        // Pagina 5: Consente il caricamento del file CSV contente i dati delle varie Utenze Idriche

        public IActionResult Upload()
        {
            // 1. Verifico se esiste una sessione attiva e che il ruolo del utente è ADMIN
            if (!VerificaSessione("ADMIN"))
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // 2. Carico gli Enti
            ViewBag.Enti = _context.Enti.ToList();
            return View();
        }


        // Fine - Pagine di navigazione

        // Inizio - Funzioni da eseguire a seconda della operazione

        // Funzione 1: Consente la creazione di una Utenza 
        [HttpPost] // Da sistemare
        public IActionResult Crea(string idAcquedotto, string matricolaContatore, int stato, DateTime periodoIniziale, DateTime? periodoFinale, string indirizzo_ubicazione, string numero_civico, DateTime? dataNascita, string tipo_utenza, string cognome, string nome, string sesso, string codice_fiscale, string? partita_iva, int idEnte, int idUser)
        {
            var nuovaUtenza = new UtenzaIdrica
            {
                idAcquedotto = idAcquedotto,
                matricolaContatore = matricolaContatore,
                stato = stato,
                periodoIniziale = periodoIniziale,
                periodoFinale = periodoFinale,
                indirizzoUbicazione = FunzioniTrasversali.rimuoviVirgolette(indirizzo_ubicazione),
                numeroCivico = FunzioniTrasversali.FormattaNumeroCivico(numero_civico),
                tipoUtenza = FunzioniTrasversali.rimuoviVirgolette(tipo_utenza),
                cognome = FunzioniTrasversali.rimuoviVirgolette(cognome),
                nome = FunzioniTrasversali.rimuoviVirgolette(nome),
                sesso = sesso,
                DataNascita = dataNascita,
                codiceFiscale = FunzioniTrasversali.rimuoviVirgolette(codice_fiscale),
                partitaIva = partita_iva,
                IdEnte = idEnte,
                IdUser = idUser,
                data_creazione = DateTime.Now,
                data_aggiornamento = null,
            };

            _context.UtenzeIdriche.Add(nuovaUtenza);
            _context.SaveChanges();
            _cache.ClearEnteCache(idEnte);

            return RedirectToAction("Show", "Utenze", new { selectedEnteId = idEnte });
        }


        // Funzione 2: Consente l'update dei dati di una Utenza

        [HttpPost]
        public IActionResult Update(int id, string idAcquedotto, int? stato, DateTime? periodoIniziale, DateTime? periodoFinale, string? matricolaContatore, string? indirizzo_ubicazione, string? numero_civico, string tipo_utenza, string? cognome, string? nome, string? sesso, string? codice_fiscale, string? partita_iva, int idEnte)
        {
            var UtenzaEsistente = _context.UtenzeIdriche.FirstOrDefault(t => t.id == id);

            if (UtenzaEsistente == null)
            {
                return RedirectToAction("Index", "Home"); // oppure restituisci una view con errore
            }

            // Aggiorna le proprietà

            UtenzaEsistente.idAcquedotto = idAcquedotto;
            UtenzaEsistente.stato = stato;
            UtenzaEsistente.periodoIniziale = periodoIniziale;
            UtenzaEsistente.periodoFinale = periodoFinale;
            UtenzaEsistente.matricolaContatore = matricolaContatore;
            UtenzaEsistente.codiceFiscale = codice_fiscale;
            UtenzaEsistente.partitaIva = partita_iva;
            UtenzaEsistente.cognome = cognome;
            UtenzaEsistente.nome = nome;
            UtenzaEsistente.sesso = sesso;
            UtenzaEsistente.indirizzoUbicazione = indirizzo_ubicazione;
            UtenzaEsistente.numeroCivico = numero_civico;
            UtenzaEsistente.tipoUtenza = tipo_utenza;
            UtenzaEsistente.data_aggiornamento = DateTime.Now;

            _context.SaveChanges();
            _cache.ClearEnteCache(idEnte);

            return RedirectToAction("Show", "Utenze", new { selectedEnteId = idEnte });
        }

        // Funzione 3: Consente di caricare le utenze del file csv sul db

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile csv_file, int selectedEnteId, int meseRiferimento, int annoRiferimento)
        {
            // Controllo se l'utente può accedere alla pagina desideratà
            if (string.IsNullOrEmpty(ruolo) || ruolo != "ADMIN")
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            // Validazione file
            if (csv_file == null || csv_file.Length == 0)
            {
                ViewBag.Message = "Seleziona un file CSV da caricare.";
                ViewBag.Enti = _context.Enti.ToList();
                return Upload();
            }

            if (Path.GetExtension(csv_file.FileName).ToLowerInvariant() != ".csv")
            {
                ViewBag.Enti = _context.Enti.ToList();
                ViewBag.Message = "Il file selezionato non è un CSV valido.";
                return Upload();
            }

            // Verifico che l'ente selezionato è valido 
            int annoMassimo = DateTime.Now.Year + 1;
            if (meseRiferimento < 1 || meseRiferimento > 12 || annoRiferimento < 2021 || annoRiferimento > annoMassimo)
            {
                ViewBag.Message = $"Mese o anno di riferimento non validi. Il mese deve essere tra 1 e 12 e l'anno tra 2021 e {annoMassimo}.";
                return Upload();
            }

            var selectedEnte = await _context.Enti.FindAsync(selectedEnteId);

            if (selectedEnte == null)
            {
                ViewBag.Message = "Ente selezionato non valido.";
                ViewBag.Enti = _context.Enti.ToList();
                return Upload();
            }

            string filePath = Path.GetTempFileName();

            try
            {
                // Salva il file temporaneamente su disco
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await csv_file.CopyToAsync(stream);
                }


                // Lettura del file CSV
                var datiComplessivi = CSVReader.LeggiFileUtenzeIdriche(filePath, selectedEnteId, _context, idUser ?? 0, annoRiferimento, meseRiferimento);

                if (datiComplessivi == null)
                {
                    ViewBag.Message = "Nessun dato valido trovato nel file CSV.";
                    return Upload();
                }

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Da Implementare aggiornamento dati UtenzeIdriche
                        var datiPresenti = false;

                        // Controllo se sono presenti dei dati da caricare sul DB

                        if (datiComplessivi.Toponimi.Count > 0)
                        {
                            datiPresenti = true;
                            foreach (var top in datiComplessivi.Toponimi)
                            {
                                _context.Toponomi.Add(top);
                            }
                        }

                        if (datiComplessivi.ToponimiDaAggiornare.Count > 0)
                        {
                            datiPresenti = true;
                            foreach (var top in datiComplessivi.ToponimiDaAggiornare)
                            {
                                top.dataAggiornamento = DateTime.Now;
                                _context.Toponomi.Update(top);
                            }
                        }

                        if (datiComplessivi.UtenzeIdriche.Count > 0)
                        {
                            datiPresenti = true;
                            // Inserimento nuove utenze
                            foreach (var utenza in datiComplessivi.UtenzeIdriche)
                            {
                                _context.UtenzeIdriche.Add(utenza);
                            }
                        }

                        if (datiComplessivi.UtenzeIdricheEsistente.Count > 0)
                        {
                            datiPresenti = true;
                            foreach (var utenza in datiComplessivi.UtenzeIdricheEsistente)
                            {
                                utenza.data_aggiornamento = DateTime.Now;
                                _context.UtenzeIdriche.Update(utenza);
                            }
                        }

                        if (datiComplessivi.UtenzeIdricheSnapshot.Count > 0)
                        {
                            datiPresenti = true;
                        }

                        // Verifico se sono non sono presenti dei dati
                        if (!datiPresenti)
                        {
                            ViewBag.Message = "Nessun dato valido trovato nel file CSV.";
                            return Upload();
                        }

                        var snapshotEsistenti = _context.UtenzeIdricheSnapshot
                            .Where(s => s.IdEnte == selectedEnteId
                                && s.AnnoRiferimento == annoRiferimento
                                && s.MeseRiferimento == meseRiferimento);

                        _context.UtenzeIdricheSnapshot.RemoveRange(snapshotEsistenti);

                        if (datiComplessivi.UtenzeIdricheSnapshot.Count > 0)
                        {
                            _context.UtenzeIdricheSnapshot.AddRange(datiComplessivi.UtenzeIdricheSnapshot);
                        }

                        // Salvataggio le modifiche iniziali
                        await _context.SaveChangesAsync();
                        transaction.Commit();  // Confermo la transizione
                        _cache.ClearEnteCache(selectedEnteId);
                        if (idUser.HasValue)
                        {
                            _cache.ClearUserCache(idUser.Value);
                        }
                    }
                    catch (Exception dbEx)
                    {
                        transaction.Rollback();
                        _logger.LogError(dbEx, "Errore durante il salvataggio dei dati nel database.");
                        ViewBag.Message = $"Errore durante il salvataggio dei dati nel database: {dbEx.Message}";
                        return Upload();
                    }
                }

                // Associazione utenze senza idToponimo
                var utenzeTopNull = new List<UtenzaIdrica>();

                if (utenzeTopNull.Count > 0)
                {
                    // Dizionario dei toponimi già presenti, con denominazione normalizzata
                    var toponimiEnte = _context.Toponomi.Where(t => t.IdEnte == selectedEnteId).ToList();

                    foreach (var utenza in utenzeTopNull)
                    {
                        // Normalizzo i valori per evitare mismatch dovuti a maiuscole/spazi/virgolette
                        var indirizzoSeparato = FunzioniTrasversali.ExtractToponimoAndCivico(utenza.indirizzoUbicazione, utenza.numeroCivico);
                        string indirizzoOriginale = indirizzoSeparato.Toponimo;
                        string indirizzoNorm = FunzioniTrasversali.NormalizeToponimo(indirizzoOriginale);

                        if (!string.IsNullOrWhiteSpace(indirizzoSeparato.CivicoEstratto))
                        {
                            _logger.LogInformation("Civico estratto da indirizzo: '{IndirizzoOriginale}' -> toponimo '{Toponimo}', civico '{Civico}'.", utenza.indirizzoUbicazione, indirizzoSeparato.Toponimo, indirizzoSeparato.CivicoEstratto);
                        }

                        var topRelativo = toponimiEnte.FirstOrDefault(t =>
                            string.Equals(t.denominazione, indirizzoOriginale, StringComparison.OrdinalIgnoreCase) ||
                            FunzioniTrasversali.NormalizeToponimo(t.denominazione) == indirizzoNorm ||
                            FunzioniTrasversali.NormalizeToponimo(t.normalizzazione) == indirizzoNorm);

                        if (topRelativo == null)
                        {
                            var candidatiCompatibili = toponimiEnte
                                .Where(t => FunzioniTrasversali.AreToponimiCompatibili(t.denominazione, indirizzoOriginale) ||
                                            FunzioniTrasversali.AreToponimiCompatibili(t.normalizzazione, indirizzoOriginale))
                                .ToList();

                            if (candidatiCompatibili.Count == 1)
                            {
                                topRelativo = candidatiCompatibili[0];
                                _logger.LogInformation("Toponimo compatibile per iniziale nome proprio: '{IndirizzoOriginale}' -> '{Toponimo}'.", indirizzoOriginale, topRelativo.denominazione);
                            }
                            else if (candidatiCompatibili.Count > 1)
                            {
                                _logger.LogWarning("Toponimo non associato automaticamente per ambiguità iniziale nome proprio: '{IndirizzoOriginale}'. Candidati: {NumeroCandidati}.", indirizzoOriginale, candidatiCompatibili.Count);
                            }
                        }

                        if (topRelativo == null)
                        {
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(indirizzoSeparato.CivicoEstratto))
                        {
                            var civicoSeparato = FunzioniTrasversali.FormattaNumeroCivico(utenza.numeroCivico);
                            if (string.IsNullOrWhiteSpace(civicoSeparato))
                            {
                                utenza.numeroCivico = indirizzoSeparato.CivicoEstratto;
                            }
                            else if (civicoSeparato != indirizzoSeparato.CivicoEstratto)
                            {
                                _logger.LogWarning("Conflitto civico: indirizzo '{Indirizzo}' contiene civico '{CivicoEstratto}', ma il campo civico separato contiene '{CivicoSeparato}'.", utenza.indirizzoUbicazione, indirizzoSeparato.CivicoEstratto, civicoSeparato);
                            }
                        }

                        if (!string.Equals(topRelativo.denominazione, indirizzoOriginale, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("Toponimo associato tramite normalizzazione: '{IndirizzoOriginale}' -> '{IndirizzoNormalizzato}'. ID toponimo: {IdToponimo}", indirizzoOriginale, indirizzoNorm, topRelativo.id);
                        }

                        // Aggiorno l'utenza con il nuovo idToponimo
                        utenza.idToponimo = topRelativo.id != null ? topRelativo.id : null;

                        _context.UtenzeIdriche.Update(utenza);
                    }
                    await _context.SaveChangesAsync();
                }

                // Messaggio da stampare 
                ViewBag.Message = $"File '{csv_file.FileName}' caricato con successo.\nNuove Utenze: {datiComplessivi.UtenzeIdriche.Count}.\tAggiornate: {datiComplessivi.UtenzeIdricheEsistente.Count}\nSnapshot {meseRiferimento:D2}/{annoRiferimento}: {datiComplessivi.UtenzeIdricheSnapshot.Count}\n";

                // Informo l'utente  se deve procedere ad Aggiornare i Toponomi

                if (datiComplessivi.countIndirizziMalFormati == null || datiComplessivi.countIndirizziMalFormati == 0)
                {
                    ViewBag.Message = ViewBag.Message + "Non sono stati riscontrati indirizzi mal formati!";
                }
                else
                {
                    ViewBag.Message = ViewBag.Message + $"Sono stati trovati {datiComplessivi.countIndirizziMalFormati} indirizzi malformati si consiglia di andare ad aggiornare i toponimi in modo tale da prevenire eventuali incongruenze durante la generazione dei domande";
                }
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
            return Upload();
        }


        public IActionResult EsportaCsv(int selectedEnteId)
        {
            if (!VerificaSessione("ADMIN") || idUser != 1)
            {
                AccountController.logFile.LogWarning("Utente non autorizzato ad esportare le utenze idriche CSV.");
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            var ente = _context.Enti.FirstOrDefault(e => e.id == selectedEnteId);
            if (ente == null)
            {
                return BadRequest("Ente non trovato!");
            }

            var dati = _context.UtenzeIdriche
                .Where(u => u.IdEnte == selectedEnteId)
                .OrderBy(u => u.idAcquedotto)
                .ThenBy(u => u.id)
                .ToList();

            var fileBytes = CsvGenerator.GeneraCsvUtenzeIdriche(dati);
            var fileName = $"UtenzeIdriche_{ente.partitaIva}_{DateTime.Now:yyyyMMddHHmmss}.csv";

            AccountController.logFile.LogInfo("L'Utente " + username + " ha esportato il file " + fileName + " per l'ente ID " + selectedEnteId);
            return File(fileBytes, "text/csv", fileName);
        }

        // Fine - Funzioni da eseguire a seconda della operazione
    }
}
