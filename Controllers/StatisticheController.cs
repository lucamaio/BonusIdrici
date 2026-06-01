using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Models;
using Data;

namespace Controllers
{
    public class StatisticheController : Controller
    {
        private readonly ILogger<StatisticheController> _logger;
        private readonly ApplicationDbContext _context;

        private string? ruolo;
        private int? idUser;
        private string? username;

        public StatisticheController(ILogger<StatisticheController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            username = HttpContext.Session.GetString("Username");
            ruolo = HttpContext.Session.GetString("Role");
            idUser = HttpContext.Session.GetInt32("idUser") ?? 0;

            if (!VerificaSessione())
            {
                username = null;
                ruolo = null;
                idUser = 0;
            }

            ViewBag.idUser = idUser;
            ViewBag.Username = username;
            ViewBag.Ruolo = ruolo;
        }

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

        public IActionResult Index()
        {
            if (!VerificaSessione("ADMIN"))
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            var enti = _context.Enti.OrderBy(e => e.nome).ToList();
            var report = _context.Reports.ToList();
            var domande = _context.Domande.ToList();

            ViewBag.TotaleEnti = enti.Count;
            ViewBag.TotaleUtenze = _context.UtenzeIdriche.Count();
            ViewBag.TotaleUtenti = _context.Users.Count();
            ViewBag.TotaleDichiaranti = _context.Dichiaranti.Count();
            ViewBag.TotaleDomande = domande.Count;
            ViewBag.TotaleReport = report.Count;
            ViewBag.TotaleToponomi = _context.Toponomi.Count();
            ViewBag.TotaleToponomiDaSistemare = _context.Toponomi.Count(t => t.normalizzazione == null);

            var anagrafePerEnte = _context.Dichiaranti
                .GroupBy(d => d.IdEnte)
                .Select(g => new { IdEnte = g.Key, Totale = g.Count() })
                .ToList();

            var utenzePerEnte = _context.UtenzeIdriche
                .GroupBy(u => u.IdEnte)
                .Select(g => new { IdEnte = g.Key, Totale = g.Count() })
                .ToList();

            var toponimiPerEnte = _context.Toponomi
                .GroupBy(t => t.IdEnte)
                .Select(g => new
                {
                    IdEnte = g.Key,
                    Totale = g.Count(),
                    DaSistemare = g.Count(t => t.normalizzazione == null)
                })
                .ToList();

            var reportPerEnte = report
                .GroupBy(r => r.idEnte)
                .Select(g => new { IdEnte = g.Key, Totale = g.Count() })
                .ToList();

            var enteCards = enti.Select(e => new
            {
                idEnte = e.id,
                nome = e.nome,
                anagrafe = anagrafePerEnte.FirstOrDefault(x => x.IdEnte == e.id)?.Totale ?? 0,
                utenze = utenzePerEnte.FirstOrDefault(x => x.IdEnte == e.id)?.Totale ?? 0,
                report = reportPerEnte.FirstOrDefault(x => x.IdEnte == e.id)?.Totale ?? 0,
                toponimi = toponimiPerEnte.FirstOrDefault(x => x.IdEnte == e.id)?.Totale ?? 0,
                toponimiDaSistemare = toponimiPerEnte.FirstOrDefault(x => x.IdEnte == e.id)?.DaSistemare ?? 0
            }).ToList();

            var domandePerEsito = domande
                .GroupBy(d => new { d.idReport, d.esito })
                .Select(g => new { g.Key.idReport, Esito = g.Key.esito, Totale = g.Count() })
                .ToList();

            var reportChart = report
                .OrderByDescending(r => r.DataCreazione)
                .Take(20)
                .OrderBy(r => r.DataCreazione)
                .Select(r =>
                {
                    var ente = enti.FirstOrDefault(e => e.id == r.idEnte);
                    return new
                    {
                        idReport = r.id,
                        idEnte = r.idEnte,
                        ente = ente?.nome ?? $"Ente {r.idEnte}",
                        data = r.DataCreazione.ToString("dd/MM/yyyy"),
                        periodo = $"{r.mese} {r.anno}",
                        stato = r.stato ?? "ND",
                        totale = domande.Count(d => d.idReport == r.id),
                        esito01 = domandePerEsito.FirstOrDefault(x => x.idReport == r.id && x.Esito == "01")?.Totale ?? 0,
                        esito02 = domandePerEsito.FirstOrDefault(x => x.idReport == r.id && x.Esito == "02")?.Totale ?? 0,
                        esito03 = domandePerEsito.FirstOrDefault(x => x.idReport == r.id && x.Esito == "03")?.Totale ?? 0,
                        esito04 = domandePerEsito.FirstOrDefault(x => x.idReport == r.id && x.Esito == "04")?.Totale ?? 0
                    };
                })
                .ToList();

            ViewBag.EnteCards = enteCards;
            ViewBag.ReportChart = reportChart;
            ViewBag.AggiornatoAlle = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            return View();
        }

        public IActionResult DettaglioEsito(int idReport, string esito)
        {
            if (!VerificaSessione("ADMIN"))
            {
                ViewBag.Message = "Utente non autorizzato ad accedere a questa pagina";
                return RedirectToAction("Index", "Home");
            }

            if (idReport <= 0 || string.IsNullOrWhiteSpace(esito))
            {
                return RedirectToAction("Index");
            }

            var report = _context.Reports.FirstOrDefault(r => r.id == idReport);
            if (report == null)
            {
                return RedirectToAction("Index");
            }

            var ente = _context.Enti.FirstOrDefault(e => e.id == report.idEnte);
            var domande = _context.Domande
                .Where(d => d.idReport == idReport && d.esito == esito)
                .OrderBy(d => d.cognomeDichiarante)
                .ThenBy(d => d.nomeDichiarante)
                .ToList();

            ViewBag.IdReport = idReport;
            ViewBag.Esito = esito;
            ViewBag.NomeEnte = ente?.nome ?? $"Ente {report.idEnte}";
            ViewBag.IdEnte = report.idEnte;
            ViewBag.Periodo = $"{report.mese} {report.anno}";
            ViewBag.DataCreazione = report.DataCreazione.ToString("dd/MM/yyyy HH:mm");
            ViewBag.Totale = domande.Count;

            return View(domande);
        }
    }
}
