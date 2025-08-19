using System;

namespace BonusIdrici2.Models.ViewModels
{
    public class RiepilogoDatiViewModel
    {
        public DateTime? DataCreazione { get; set; }
        public int NumeroDatiInseriti { get; set; }
        
        public int Iduser { get; set; }
        public string Username { get; set; }

    }
}