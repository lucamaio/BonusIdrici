using System;

namespace BonusIdrici2.Models.ViewModels
{
    public class RiepilogoDatiViewModel
    {
        public int? id { get; set; }
        public DateTime? DataCreazione { get; set; }

        public string idAto { get; set; }

        public string? codiceBonus { get; set; }

        public string? esitoStr { get; set; }  // esito "Si" o "No"

        public string? esito { get; set; }  // (1<valore>5) 

         public int? idFornitura { get; set; }

        public string? codiceFiscale { get; set; }

        public int? numeroComponenti { get; set; }

        public string? annoValidita { get; set; }

        public int? serie { get; set; }

        public int? mc { get; set; }

        public int? NumeroDatiInseriti { get; set; }

        public int? Iduser { get; set; }

        public int? IdEnte { get; set; }
        public string? Username { get; set; }

    }
}