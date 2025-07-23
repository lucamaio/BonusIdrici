using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BonusIdrici2.Models; 
using BonusIdrici2.Data;
using System.IO;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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
            return View();
        }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpPost]
        public IActionResult creaEnte(Ente ente)
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