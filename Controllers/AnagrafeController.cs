using Microsoft.AspNetCore.Mvc;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;

using BonusIdrici2.Models.ViewModels;

namespace BonusIdrici2.Controllers
{
    public class AnagrafeController : Controller
    {
        private readonly ILogger<AnagrafeController> _logger;
        private readonly ApplicationDbContext _context;
        public AnagrafeController(ILogger<AnagrafeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Pagine di navigazione

        public IActionResult Index()
        {
            if (!VerificaSessione())
            {
                //ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            var ruolo = HttpContext.Session.GetString("Role");
            int idUser = (int) HttpContext.Session.GetInt32("idUser");
            List<Ente> enti = new List<Ente>();

            if (ruolo == "OPERATORE")
            {
                enti = FunzioniTrasversali.GetEnti(_context, idUser);
                if (enti.Count == 1)
                {
                    return Show(enti[0].id);
                }
            }
            else
            {
                enti = _context.Enti.OrderBy(e => e.nome).ToList();
            }            
            
            ViewBag.Enti = enti;
            return View();
        }

        // Pagina per la visualizzazione dell'anagrafe

        public IActionResult Show(int selectedEnteId)
        {
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

        public IActionResult Create(int idEnte)
        {
            ViewBag.IdEnte = idEnte;
            return View();
        }

        [HttpPost]
        public IActionResult crea(string cognome, string nome, string codice_fiscale, string sesso, DateTime? data_nascita, string? comune_nascita, string indirizzo_residenza, string numero_civico, int idEnte)
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
                data_creazione = DateTime.Now,
                data_aggiornamento = null
            };

            _context.Dichiaranti.Add(nuovaPersona);
            _context.SaveChanges();

            return RedirectToAction("Show", "Anagrafe", new { selectedEnteId = idEnte });
        }

        public IActionResult Modifica(int id)
        {
            ViewBag.id = id;
            List<Dichiarante> dichiarante = _context.Dichiaranti.Where(s => s.id == id).ToList();
            ViewBag.Dichiarante = dichiarante.First();
            return View();
        }

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
        
        // Funzione che controlla se esiste una funzione e se il ruolo e uguale a quello richiesto per accedere alla pagina desiderata
        public bool VerificaSessione(string ruoloRichiesto = null)
        {
            string username = HttpContext.Session.GetString("Username");
            string ruolo = HttpContext.Session.GetString("Role");

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

    }
}