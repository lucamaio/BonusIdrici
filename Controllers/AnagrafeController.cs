using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;
using System.IO;
using BonusIdrici2.Models.ViewModels;

namespace BonusIdrici2.Controllers
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
                username = HttpContext.Session.GetString("Username");
                ruolo = HttpContext.Session.GetString("Role");
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
                Sesso = x.Sesso ?? string.Empty,
                DataNascita = x.DataNascita,
                ComuneNascita = x.ComuneNascita ?? string.Empty,
                IndirizzoResidenza = x.IndirizzoResidenza,
                NumeroCivico = x.NumeroCivico,
                CodiceFamiglia = x.CodiceFamiglia,
                Parentela = x.Parentela,
                CodiceFiscaleIntestatarioScheda = x.CodiceFiscaleIntestatarioScheda,
                NumeroComponenti = x.NumeroComponenti,
                data_creazione = x.data_creazione,
                data_aggiornamento = x.data_aggiornamento,
                IdEnte = x.IdEnte
            }).ToList();


            ViewBag.SelectedEnteId = selectedEnteId;
            ViewBag.SelectedEnteNome = _context.Enti.FirstOrDefault(e => e.id == selectedEnteId)?.nome ?? "Ente Sconosciuto";

            return View("Show", viewModelList);
        }

        // Pagina per la creazione di un nuovo dichiarante
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

        // Pagina per la modifica di un dichiarante

        public IActionResult Modifica(int id)
        {
            if (!VerificaSessione()) 
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }
            
            ViewBag.id = id;
            List<Dichiarante> dichiarante = _context.Dichiaranti.Where(s => s.id == id).ToList();
            ViewBag.Dichiarante = dichiarante.First();
            return View();
        }

        // Fine - Pagine Navigazione

        // Inizio - Funzioni da eseguire a seconda della operazione

        // Funzione che consente di andare a creare un nuovo dichiarante da i dati provenienti dal form Anagrafe/Create.cshtml
        [HttpPost]
        public IActionResult Crea(string cognome, string nome, string codice_fiscale, string sesso, DateTime? data_nascita, string? comune_nascita, string indirizzo_residenza, string numero_civico, int idEnte,int idUser)
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
                IdUser = idUser,
                data_creazione = DateTime.Now,
                data_aggiornamento = null
            };

            _context.Dichiaranti.Add(nuovaPersona);
            _context.SaveChanges();

            return RedirectToAction("Show", "Anagrafe", new { selectedEnteId = idEnte });
        }

        // Funzione che consente di andare a creare un nuovo dichiarante da i dati provenienti dal form Anagrafe/Modifica.cshtml

        [HttpPost]
        public IActionResult Update(int id, string cognome, string nome, string codice_fiscale, string sesso, DateTime? data_nascita, string? comune_nascita, string indirizzo_residenza, string numero_civico, int idEnte)
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

        // Fine - Funzioni da eseguire a seconda della operazione

    }
}