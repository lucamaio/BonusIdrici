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

            return View("Index",viewModelList);
        }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpPost]
        public IActionResult Create(Ente ente)
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
            return View("Index","Enti");
        }



    }
    
}