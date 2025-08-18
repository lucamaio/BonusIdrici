using Microsoft.AspNetCore.Mvc;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;

using BonusIdrici2.Models.ViewModels;

namespace BonusIdrici2.Controllers
{
    public class UtenzeController : Controller
    {
        private readonly ILogger<UtenzeController> _logger;
        private readonly ApplicationDbContext _context;
        public UtenzeController(ILogger<UtenzeController> logger, ApplicationDbContext context)
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
            int idUser = (int)HttpContext.Session.GetInt32("idUser");
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

        // Pagina per la visualizzazione dell'Utenze Idrica

        public IActionResult Show(int selectedEnteId)
        {
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

        public IActionResult Create(int idEnte)
        {
            ViewBag.IdEnte = idEnte;
            return View();
        }

        [HttpPost]
        public IActionResult Crea(
             string cognome,
             string nome,
             string codice_fiscale,
             string sesso,
             DateTime? data_nascita,
             string? comune_nascita,
             string indirizzo_residenza,
             string numero_civico,
             int idEnte)
        {
            var nuovaPersona = new Dichiarante
            {
                Cognome = FunzioniTrasversali.rimuoviVirgolette(cognome).ToUpper(),
                Nome = FunzioniTrasversali.rimuoviVirgolette(nome).ToUpper(),
                CodiceFiscale = codice_fiscale.ToUpper(),
                Sesso = FunzioniTrasversali.rimuoviVirgolette(sesso).ToUpper(),
                DataNascita = data_nascita,
                ComuneNascita = FunzioniTrasversali.rimuoviVirgolette(comune_nascita)?.ToUpper(),
                IndirizzoResidenza = FunzioniTrasversali.rimuoviVirgolette(indirizzo_residenza).ToUpper(),
                NumeroCivico = FunzioniTrasversali.FormattaNumeroCivico(numero_civico)?.ToUpper(),
                IdEnte = idEnte,
                data_creazione = DateTime.Now,
                data_aggiornamento = null
            };

            _context.Dichiaranti.Add(nuovaPersona);
            _context.SaveChanges();

            return RedirectToAction("Show", "Utenze", new { selectedEnteId = idEnte });
        }


        public IActionResult Modifica(int id)
        {
            ViewBag.id = id;
            List<UtenzaIdrica> utenza = _context.UtenzeIdriche.Where(s => s.id == id).ToList();
            ViewBag.Utenza = utenza.First();
            return View();
        }

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