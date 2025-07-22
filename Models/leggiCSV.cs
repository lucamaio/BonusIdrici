using System.Collections.Generic;

// Non è più necessario importare 'Dichiarante' e 'Atto' come namespace
// perché useremo i nomi completi qualificati direttamente.

namespace leggiCSV
{
    public class DatiCsvCompilati // Nome più descrittivo per il contenitore
    {
        public List<Dichiarante.Dichiarante> Dichiaranti { get; set; }
        public List<BonusIdrici2.Models.UtenzaIdrica> UtenzeIdriche { get; set; }
        public List<Atto.Atto> Atti { get; set; }

        public List<BonusIdrici2.Models.Report> reports { get; set; } = new List<BonusIdrici2.Models.Report>();

        public DatiCsvCompilati() // Costruttore per inizializzare le liste
        {
            Dichiaranti = new List<Dichiarante.Dichiarante>();
            UtenzeIdriche = new List<BonusIdrici2.Models.UtenzaIdrica>();
            Atti = new List<Atto.Atto>();
            reports = new List<BonusIdrici2.Models.Report>();
        }
    }
}