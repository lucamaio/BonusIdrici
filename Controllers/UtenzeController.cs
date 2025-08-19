using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;
using System.IO;
using BonusIdrici2.Models.ViewModels;

namespace BonusIdrici2.Controllers
{
    public class UtenzeController : Controller
    {
        // Dichiarazione delle variabili di istanza
        private readonly ILogger<UtenzeController> _logger;
        private readonly ApplicationDbContext _context;

        private string? ruolo;
        private int idUser;
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
                idUser = (int)HttpContext.Session.GetInt32("idUser");
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
            List<UtenzaIdrica> utenza = _context.UtenzeIdriche.Where(s => s.id == id).ToList();
            ViewBag.Utenza = utenza.First();
            return View();
        }

        // Fine - Pagine di navigazione

        // Inizio - Funzioni da eseguire a seconda della operazione

        // Funzione 1: Consente la creazione di una Utenza 
        [HttpPost] // Da sistemare
        public IActionResult Crea(string idAcquedotto,string matricolaContatore,int stato,DateTime periodoIniziale, DateTime? periodoFinale, string indirizzo_ubicazione, string numero_civico,DateTime? dataNascita, string tipo_utenza, string cognome, string nome, string sesso, string codice_fiscale, string? partita_iva, int idEnte, int idUser)
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
        
        // Fine - Funzioni da eseguire a seconda della operazione
    }
}