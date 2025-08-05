using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;
using System.IO;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
            List<Ente> enti = _context.Enti.OrderBy(e => e.nome).ToList();
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
                return View("Index","Anagrafe");
            }

            var dati = _context.Dichiaranti.Where(r => r.IdEnte == selectedEnteId).ToList();

            var viewModelList = dati.Select(x => new AnagrafeViewModel
            {
                id= x.id,
                Cognome = x.Cognome,
                Nome = x.Nome,
                CodiceFiscale = x.CodiceFiscale,
                Sesso = x.Sesso,
                DataNascita = x.DataNascita,
                ComuneNascita = x.ComuneNascita,
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
                Cognome = cognome,
                Nome = nome,
                CodiceFiscale = codice_fiscale,
                Sesso = sesso,
                DataNascita = data_nascita,
                ComuneNascita = comune_nascita,
                IndirizzoResidenza = indirizzo_residenza,
                NumeroCivico = numero_civico,
                IdEnte = idEnte,
                data_creazione = DateTime.Now,
                data_aggiornamento =null
            };

            _context.Dichiaranti.Add(nuovaPersona);
            _context.SaveChanges();

            return RedirectToAction("Show", "Anagrafe", new { selectedEnteId = idEnte });
        }
    }
}