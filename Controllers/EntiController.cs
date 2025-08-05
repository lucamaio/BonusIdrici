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
    public class EntiController : Controller
    {
        private readonly ILogger<EntiController> _logger;
        private readonly ApplicationDbContext _context; // Inietta il DbContext
        public EntiController(ILogger<EntiController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Pagine di navigazione

        public IActionResult Index()
        {
            var dati = _context.Enti.ToList();
            // Console.WriteLine(dati.Count());
            var viewModelList = dati.Select(x => new EntiViewModel
            {
                id = x.id,
                nome = x.nome,
                istat = x.istat,
                partitaIva = x.partitaIva,
                CodiceFiscale = x.CodiceFiscale,
                Cap = x.Cap,
                Provincia = x.Provincia,
                Regione = x.Regione,
            }).ToList();

            return View("Index", viewModelList);
        }

        public IActionResult Create()
        {
            return View();
        }

        // Funzione che viene eseguita dopo aver compilato il form per la creazione di un ente

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpPost]
        public IActionResult Crea(Ente ente)
        {
            if (ModelState.IsValid)
            {
                // Logica per salvare l'ente nel database
                _context.Enti.Add(ente);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Ente creato con successo.";
                return RedirectToAction("Index", "Home"); // oppure una vista di conferma
            }
            // In caso di errore, ritorna la stessa vista con il modello
            return View("Index", "Enti");
        }

        // Funzione che mostra le informazioni dell'ente da modificare

        public IActionResult Modifica(int id, string nome, string istat, string partitaIva, string cap, string? CodiceFiscale, string? provincia, string? regione, bool? Nostro = true)
        {
            ViewBag.id = id;
            ViewBag.nome = nome;
            ViewBag.istat = istat;
            ViewBag.partitaIva = partitaIva;
            ViewBag.Cap = cap;
            ViewBag.provincia = provincia;
            ViewBag.regione = regione;
            ViewBag.CodiceFiscale = CodiceFiscale;
            ViewBag.Nostro = Nostro;
            return View();
        }

         // Funzione che viene eseguita per aggiornare i dati del ente con queli inseriti nel form

        [HttpPost]
        public IActionResult Update(int id, string nome, string istat, string partitaIva, string cap, string? CodiceFiscale, string? provincia, string? regione, bool? Nostro)
        {
            var enteEsistente = _context.Enti.FirstOrDefault(s => s.id == id);

            if (enteEsistente == null)
            {
                return RedirectToAction("Index", "Home");
            }

            enteEsistente.nome = nome.Trim().ToUpper();
            enteEsistente.istat = istat.Trim().ToUpper();
            enteEsistente.partitaIva = partitaIva.Trim().ToUpper();
            enteEsistente.Cap = cap.Trim();
            enteEsistente.Provincia = provincia.Trim().ToUpper();
            enteEsistente.Regione = regione.Trim().ToUpper();
            enteEsistente.CodiceFiscale = CodiceFiscale.Trim().ToUpper();
            enteEsistente.Nostro = Nostro;
            enteEsistente.data_aggiornamento = DateTime.Now;

            _context.SaveChanges();
            return RedirectToAction("Index", "Enti");
        }
    }
}