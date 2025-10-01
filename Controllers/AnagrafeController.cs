using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Models; 
using Data;
using System.IO;
using Models.ViewModels;

namespace Controllers
{
    public class AnagrafeController : Controller
    {
        // Dichiarazione delle variabili di istanza
        private readonly ILogger<AnagrafeController> _logger;
        private readonly ApplicationDbContext _context;
        
        private string? ruolo;
        private int idUser;
        private string? username;

        // Costruttore
        public AnagrafeController(ILogger<AnagrafeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
            
            if (VerificaSessione())
            {
                username =  (string?) HttpContext.Session.GetString("Username");
                ruolo = (string?) HttpContext.Session.GetString("Role");
                idUser = (int) HttpContext.Session.GetInt32("idUser");
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
                idUser = 0;
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


        // Inizio Pagine di navigazione

        // Pagina home per la selezione del ente 
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

        // Pagina per la visualizzazione dell'anagrafe

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
                return View("Index", "Anagrafe");
            }

            var dati = _context.Dichiaranti.Where(r => r.IdEnte == selectedEnteId).ToList();

            var viewModelList = dati.Select(x => new AnagrafeViewModel
            {
                id = x.id,
                Cognome = x.Cognome,
                Nome = x.Nome,
                CodiceFiscale = x.CodiceFiscale,
                Sesso = x.Sesso,
                IndirizzoResidenza = x.IndirizzoResidenza,
                NumeroCivico = x.NumeroCivico,
                IdEnte = x.IdEnte
            }).ToList();


            ViewBag.SelectedEnteId = selectedEnteId;
            ViewBag.SelectedEnteNome = _context.Enti.FirstOrDefault(e => e.id == selectedEnteId)?.nome ?? "Ente Sconosciuto";

            return View("Show", viewModelList);
        }

        // Pagina per la creazione di un nuovo dichiarante
        public IActionResult Create(int idEnte, int? codiceFamiglia, string? codiceIntestarioScheda, string? indirizzoResidenza, string? civico)
        {
            if (!VerificaSessione()) 
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.IdEnte = idEnte;
            ViewBag.codiceFamiglia = codiceFamiglia;
            ViewBag.indirizzoResidenza = indirizzoResidenza;
            ViewBag.NumeroCivico = civico;
            if (codiceFamiglia != null)
            {
                ViewBag.Parentele = _context.Dichiaranti.Select(s => s.Parentela).Distinct().ToList();
                ViewBag.codiceIntestarioScheda = codiceIntestarioScheda;
            }
            return View();
        }

        // Pagina per la modifica di un dichiarante

        public IActionResult Modifica(int id)
        {
            if (!VerificaSessione()) 
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }
            
            ViewBag.id = id;
            var dichiarante = _context.Dichiaranti.FirstOrDefault(s => s.id == id);
            var parenti = _context.Dichiaranti.Where(s => (s.CodiceFamiglia == dichiarante.CodiceFamiglia || s.CodiceFiscaleIntestatarioScheda == dichiarante.CodiceFiscaleIntestatarioScheda) && s.id != id).ToList();
            //var componenti = _context.Dichiaranti.Where(s => s.CodiceFamiglia == dichiarante.CodiceFamiglia || s.CodiceFiscaleIntestatarioScheda == dichiarante.CodiceFiscaleIntestatarioScheda).Count();
            ViewBag.Dichiarante = dichiarante;
            ViewBag.Parenti = parenti;
            //ViewBag.Componenti = componenti;
            return View();
        }

        // Pagina 5: Consente di caricare il file contente l'anagrafe di un ente
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


        // Fine - Pagine Navigazione

        // Inizio - Funzioni da eseguire a seconda della operazione

        // Funzione 1: che consente di andare a creare un nuovo dichiarante da i dati provenienti dal form Anagrafe/Create.cshtml
        [HttpPost]
        public IActionResult Crea(string cognome, string nome, string codice_fiscale, string sesso, DateTime data_nascita, string? comune_nascita, string indirizzo_residenza, string numero_civico, int idEnte, int? codiceFamiglia, string? parentela, string? codiceIntestarioScheda)
        {
            var nuovaPersona = new Dichiarante
            {
                Cognome = FunzioniTrasversali.rimuoviVirgolette(cognome).ToUpper(),
                Nome = FunzioniTrasversali.rimuoviVirgolette(nome).ToUpper(),
                CodiceFiscale = codice_fiscale.ToUpper(),
                Sesso = FunzioniTrasversali.rimuoviVirgolette(sesso).ToUpper(),
                DataNascita = data_nascita,
                ComuneNascita = FunzioniTrasversali.rimuoviVirgolette(comune_nascita).ToUpper(),
                IndirizzoResidenza = indirizzo_residenza.ToUpper(),
                NumeroCivico = FunzioniTrasversali.FormattaNumeroCivico(numero_civico).ToUpper(),
                IdEnte = idEnte,
                CodiceFamiglia = codiceFamiglia,
                Parentela = parentela,
                CodiceFiscaleIntestatarioScheda = codiceIntestarioScheda,
                IdUser = idUser,
                data_creazione = DateTime.Now,
                data_aggiornamento = null
            };

            _context.Dichiaranti.Add(nuovaPersona);
            _context.SaveChanges();
            if (codiceFamiglia == null)
            {
                return RedirectToAction("Modifica", "Anagrafe", new { id = nuovaPersona.id });
            }
            var dichiarantiNucleo = _context.Dichiaranti.Where(s => s.CodiceFamiglia == codiceFamiglia).ToList();
            // aggiorno il numero dei componeti del nucleo
            foreach (var membro in dichiarantiNucleo)
            {
                membro.NumeroComponenti = membro.NumeroComponenti + 1;
                membro.data_aggiornamento = DateTime.Now;
                _context.Dichiaranti.Update(membro);
            }
            _context.SaveChanges();

            return RedirectToAction("Show", "Anagrafe", new { selectedEnteId = idEnte });
        }

        // Funzione che consente di andare a creare un nuovo dichiarante da i dati provenienti dal form Anagrafe/Modifica.cshtml

        [HttpPost]
        public IActionResult Update(int id, string cognome, string nome, string codice_fiscale, string sesso, DateTime data_nascita, string? comune_nascita, string indirizzo_residenza, string numero_civico, int idEnte)
        {
            var DichiaranteEsistente = _context.Dichiaranti.FirstOrDefault(t => t.id == id);

            if (DichiaranteEsistente == null)
            {
                return RedirectToAction("Index", "Home"); // oppure restituisci una view con errore
            }

            // Aggiorna le proprietà
            DichiaranteEsistente.Cognome = FunzioniTrasversali.rimuoviVirgolette(cognome).ToUpper();
            DichiaranteEsistente.Nome = FunzioniTrasversali.rimuoviVirgolette(nome).ToUpper();
            DichiaranteEsistente.Sesso = FunzioniTrasversali.rimuoviVirgolette(sesso).ToUpper();
            DichiaranteEsistente.CodiceFiscale = FunzioniTrasversali.rimuoviVirgolette(codice_fiscale).ToUpper();
            DichiaranteEsistente.DataNascita = data_nascita;
            DichiaranteEsistente.ComuneNascita = FunzioniTrasversali.rimuoviVirgolette(comune_nascita).ToUpper();
            DichiaranteEsistente.IndirizzoResidenza = FunzioniTrasversali.rimuoviVirgolette(indirizzo_residenza).ToUpper();
            DichiaranteEsistente.NumeroCivico = FunzioniTrasversali.FormattaNumeroCivico(numero_civico).ToUpper();
            DichiaranteEsistente.data_aggiornamento = DateTime.Now;

            _context.SaveChanges();

            return RedirectToAction("Show", "Anagrafe", new { selectedEnteId = idEnte });
        }

        // Funzione 2: consente di caricare l'anagrafe di un ente partendo da un file csv

         [HttpPost]
        public async Task<IActionResult> Upload(IFormFile csv_file, int selectedEnteId) // Il nome del parametro deve corrispondere al 'name' dell'input file nel form
        {
            // Controllo se l'utente può accedere alla pagina desideratà
            if (string.IsNullOrEmpty(ruolo) || ruolo != "ADMIN")
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            if (csv_file == null || csv_file.Length == 0)
            {
                ViewBag.Message = "Seleziona un file CSV da caricare.";
                return Upload(); // Torna alla pagina di upload con un messaggio
            }

            // Validazione del tipo di file (opzionale ma consigliata)
            if (Path.GetExtension(csv_file.FileName).ToLowerInvariant() != ".csv")
            {
                ViewBag.Message = "Il file selezionato non è un CSV valido.";
                return Upload();
            }

            var selectedEnte = await _context.Enti.FindAsync(selectedEnteId);

            if (selectedEnte == null)
            {
                ViewBag.Message = "Ente selezionato non valido.";
                return Upload();
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
                var datiComplessivi = CSVReader.LoadAnagrafe(filePath, selectedEnteId, _context, idUser);

                // Inizia una transazione per assicurare che tutti i dati vengano salvati
                // o nessuno in caso di errore. (Opzionale ma buona pratica per operazioni multiple)
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        bool trovati = false;
                        // 1. Verifico se ci sono dati da salvare
                        if (datiComplessivi.Dichiaranti.Count > 0)
                        {
                            trovati = true;
                            // Salva i nuovi dichiaranti nel database
                            foreach (var dichiarante in datiComplessivi.Dichiaranti)
                            {
                                _context.Dichiaranti.Add(dichiarante);
                            }
                        }
                        // 2. Verifico se ci sono dichiaranti da aggiornare
                        if (datiComplessivi.DichiarantiDaAggiornare.Count > 0)
                        {
                            trovati = true;
                            // Aggiorna i dichiaranti esistenti
                            foreach (var dichiarante in datiComplessivi.DichiarantiDaAggiornare)
                            {
                                _context.Dichiaranti.Update(dichiarante);
                            }
                        }
                        if(!trovati)
                        {
                            ViewBag.Message = "Nessun dato valido trovato nel file CSV.";
                            return Upload(); // Torna alla pagina di upload con un messaggio
                        }
                        
                        await _context.SaveChangesAsync();

                        transaction.Commit(); // Conferma la transazione se tutto è andato bene
                        ViewBag.Message = $"Dati salvati con successo!\nAggiunti: {datiComplessivi.Dichiaranti.Count}, Aggiornati: {datiComplessivi.DichiarantiDaAggiornare.Count}";
                    }
                    catch (Exception dbEx)
                    {
                        transaction.Rollback(); // Annulla la transazione in caso di errore
                        _logger.LogError(dbEx, "Errore durante il salvataggio dei dati nel database.");
                        ViewBag.Message = $"Errore durante il salvataggio dei dati nel database: {dbEx.Message}";
                        return Upload(); // Torna alla pagina di upload con il messaggio di errore
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione del file CSV.");
                ViewBag.Message = $"Errore durante l'elaborazione del file CSV: {ex.Message}";
                return Upload(); // Torna alla pagina di upload con il messaggio di errore
            }
            finally
            {
                // Assicurati di eliminare il file temporaneo
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            return Upload(); // Torna alla pagina di upload con il messaggio di stato
        }

        // Fine - Funzioni da eseguire a seconda della operazione

    }
}