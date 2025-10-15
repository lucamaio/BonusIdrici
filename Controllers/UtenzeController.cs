using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Models; 
using Data;
using System.IO;
using Models.ViewModels;

namespace Controllers
{
    public class UtenzeController : Controller
    {
        // Dichiarazione delle variabili di istanza
        private readonly ILogger<UtenzeController> _logger;
        private readonly ApplicationDbContext _context;

        private string? ruolo;
        private int? idUser;
        private string? username;

        // Costruttore

        public UtenzeController(ILogger<UtenzeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;

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

            enti = _context.Enti.OrderBy(e => e.nome).ToList();
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
                ViewBag.Enti = _context.Enti.OrderBy(e => e.nome).ToList();
                ViewBag.Message = "Per favore, seleziona un ente valido.";
                return View("Index", "Utenze");
            }

            var dati = _context.UtenzeIdriche.Where(r => r.IdEnte == selectedEnteId).ToList();

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
            ViewBag.SelectedEnteNome = _context.Enti.FirstOrDefault(e => e.id == selectedEnteId)?.nome ?? "Ente Sconosciuto";

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

            return RedirectToAction("Show", "Utenze", new { selectedEnteId = idEnte });
        }

        // Funzione 3: Consente di caricare le utenze del file csv sul db

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile csv_file, int selectedEnteId)
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
                var datiComplessivi = CSVReader.LeggiFileUtenzeIdriche(filePath, selectedEnteId, _context, idUser ?? 0);

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
                                top.data_aggiornamento = DateTime.Now;
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

                        // Verifico se sono non sono presenti dei dati
                        if (!datiPresenti)
                        {
                            ViewBag.Message = "Nessun dato valido trovato nel file CSV.";
                            return Upload();
                        }

                        // Salvataggio le modifiche iniziali
                        await _context.SaveChangesAsync();
                        transaction.Commit();  // Confermo la transizione
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
                var utenzeTopNull = _context.UtenzeIdriche.Where(s => s.idToponimo == null && s.IdEnte == selectedEnteId).ToList();

                if (utenzeTopNull.Count > 0)
                {
                    // Dizionario dei toponimi già presenti, con denominazione normalizzata
                    foreach (var utenza in utenzeTopNull)
                    {
                        // Normalizzo i valori per evitare mismatch dovuti a maiuscole/spazi/virgolette
                        string indirizzoNorm = FunzioniTrasversali.rimuoviVirgolette(utenza.indirizzoUbicazione ?? "").ToUpper();

                        var topRelativo = _context.Toponomi.FirstOrDefault(t => t.IdEnte == selectedEnteId && t.denominazione == indirizzoNorm);

                        if (topRelativo == null)
                        {
                            continue;
                        }

                        // Aggiorno l'utenza con il nuovo idToponimo
                        utenza.idToponimo = topRelativo.id != null ? topRelativo.id : null;

                        _context.UtenzeIdriche.Update(utenza);
                    }
                    await _context.SaveChangesAsync();
                }

                // Messaggio da stampare 
                ViewBag.Message = $"File '{csv_file.FileName}' caricato con successo.\nNuove Utenze: {datiComplessivi.UtenzeIdriche.Count}.\tAggiornate: {datiComplessivi.UtenzeIdricheEsistente.Count}\n";

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


        // Fine - Funzioni da eseguire a seconda della operazione
    }
}