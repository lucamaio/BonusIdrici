// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Mvc.Filters;
// using Models; 
// using Data;
// using System.IO;
// using Models.ViewModels;

// namespace Controllers
// {
//     public class StatisticheController : Controller
//     {
//         // Dichiarazione delle variabili di istanza
//         private readonly ILogger<StatisticheController> _logger;
//         private readonly ApplicationDbContext _context;

//         private string? ruolo;
//         private int? idUser;
//         private string? username;

//         // Costruttore

//         public StatisticheController(ILogger<StatisticheController> logger, ApplicationDbContext context)
//         {
//             _logger = logger;
//             _context = context;

//             if (VerificaSessione())
//             {
//                 username = HttpContext.Session.GetString("Username");
//                 ruolo = HttpContext.Session.GetString("Role");
//                 idUser = HttpContext.Session.GetInt32("idUser");
//             }
//         }

//         // Funzione che inizializza le variabili con i dati della sessione

//         public override void OnActionExecuting(ActionExecutingContext context)
//         {
//             base.OnActionExecuting(context);

//             // Ora HttpContext è disponibile
//             username = HttpContext.Session.GetString("Username");
//             ruolo = HttpContext.Session.GetString("Role");
//             idUser = HttpContext.Session.GetInt32("idUser") ?? 0;

//             if (!VerificaSessione())
//             {
//                 username = null;
//                 ruolo = null;
//                 idUser = 0;
//             }

//             // Così le variabili sono disponibili in tutte le viste
//             ViewBag.idUser = idUser;
//             ViewBag.Username = username;
//             ViewBag.Ruolo = ruolo;
//         }

//         // Funzione che verifica se esiste una funzione ed il ruolo e quello richiesto per accedere alla pagina

//         public bool VerificaSessione(string? ruoloRichiesto = null)
//         {
//             if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(ruolo))
//             {
//                 return false;
//             }

//             if (!string.IsNullOrEmpty(ruoloRichiesto) && ruolo != ruoloRichiesto)
//             {
//                 return false;
//             }

//             return true;
//         }

//         // Inizio - Pagine di navigazione

//         // Pagina 1: Pagina Home che consente la selezione di un ente per poi visualizzarne le utenze idriche
//         public IActionResult Index()
//         {
//             if (!VerificaSessione("ADMIN"))
//             {
//                 ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
//                 return RedirectToAction("Index", "Home");
//             }

//             // Totali principali
//             ViewBag.TotaleEnti = _context.Enti.Count();
//             ViewBag.TotaleUtenze = _context.UtenzeIdriche.Count();
//             ViewBag.TotaleUtenti = _context.Users.Count();
//             ViewBag.TotaleDichiaranti = _context.Dichiaranti.Count();
//             ViewBag.TotaleDomande = _context.Domande.Count();
//             ViewBag.TotaleToponomi = _context.Toponomi.Count();
//             ViewBag.TotaleToponomiDaSistemare = _context.Toponomi.Count(t => t.normalizzazione == null);
            
//             // Grafico 1: Elaborazioni per ente
//            var domandePerEnte = _context.Domande
//                 .GroupBy(r => new { r.IdEnte, r.DataCreazione }) // raggruppa per idEnte e data
//                 .Select(g => new
//                 {
//                     idEnte = g.Key.IdEnte,
//                     DataCreazione = g.Key.DataCreazione,
//                     Totale = g.Count(),
//                     NomeEnte = _context.Enti
//                             .Where(e => e.id == g.Key.IdEnte)
//                             .Select(e => e.nome)
//                             .FirstOrDefault()
//                 })
//                 .ToList();

//             // Per visualizzare nel grafico: usa solo idEnte come label
//             ViewBag.DomandeLabels = domandePerEnte.Select(x => x.NomeEnte.ToString()).ToList();
//             ViewBag.DomandeValues = domandePerEnte.Select(x => x.Totale).ToList();

//             // Grafico 2: Domande per esito

//              var domandePerEsito = _context.Domande
//                 .GroupBy(d => d.esito) // supponendo campo Esito = "Accettata", "Rifiutata", "In attesa"
//                 .Select(g => new { esito = g.Key, Totale = g.Count() })
//                 .ToList();

//             ViewBag.DomandeLabels = domandePerEsito.Select(x => x.esito).ToList();
//             ViewBag.DomandeValues = domandePerEsito.Select(x => x.Totale).ToList();

//             // Grafico 3: Utenze Idriche per ente
            
//            var utenzeIdrichePerEnte = _context.UtenzeIdriche
//             .GroupBy(u => u.IdEnte)
//             .Select(g => new 
//             {
//                 IdEnte = g.Key,
//                 Totale = g.Count(),
//                 NomeEnte = _context.Enti
//                             .Where(e => e.id == g.Key)
//                             .Select(e => e.nome)
//                             .FirstOrDefault()
//             })
//             .ToList();

            
//             // Labels con i nomi degli enti
//             ViewBag.UtenzeIdricheLabels = utenzeIdrichePerEnte.Select(x => x.NomeEnte).ToList();

//             // Valori con il totale delle utenze
//             ViewBag.UtenzeIdricheValues = utenzeIdrichePerEnte.Select(x => x.Totale).ToList();

//             // Grafico 4: Anagrafe per ente

//             var anagrafePerEnte = _context.Dichiaranti
//             .GroupBy(d => d.IdEnte)
//             .Select(g => new 
//             {
//                 IdEnte = g.Key,
//                 Totale = g.Count(),
//                 NomeEnte = _context.Enti
//                             .Where(e => e.id == g.Key)
//                             .Select(e => e.nome)
//                             .FirstOrDefault()
//             })
//             .ToList();

//             // Dati per grafico: utenze idriche per ente

//             ViewBag.AnagrafeLabels = anagrafePerEnte.Select(x => x.NomeEnte).ToList();
//             ViewBag.AnagrafeValues = anagrafePerEnte.Select(x => x.Totale).ToList();

//             // Grafico 5: Toponimi per ente
            
//             var toponimiPerEnte = _context.Toponomi
//             .GroupBy(t => t.IdEnte)
//             .Select(g => new 
//             {
//                 IdEnte = g.Key,
//                 Totale = g.Count(),
//                 NomeEnte = _context.Enti
//                             .Where(e => e.id == g.Key)
//                             .Select(e => e.nome)
//                             .FirstOrDefault()
//             })
//             .ToList();
//             // Dati per grafico: toponimi per ente
//             ViewBag.ToponimiLabels = toponimiPerEnte.Select(x => x.NomeEnte).ToList();
//             ViewBag.ToponimiValues = toponimiPerEnte.Select(x => x.Totale).ToList();

//             return View();
           
//         }


//         // Fine - Pagine di navigazione

//         // Inizio - Funzioni da eseguire a seconda della operazione
        

//         // Fine - Funzioni da eseguire a seconda della operazione
//     }
// }