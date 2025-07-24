using System.Collections.Generic;
using BonusIdrici2.Models;

// Non è più necessario importare 'Dichiarante' e 'Atto' come namespace
// perché useremo i nomi completi qualificati direttamente.

namespace leggiCSV
{
    public class DatiCsvCompilati // Nome più descrittivo per il contenitore
    {
        public List<Dichiarante> Dichiaranti { get; set; }
        public List<UtenzaIdrica> UtenzeIdriche { get; set; }
        public List<Atto.Atto> Atti { get; set; }

        public List<Report> reports { get; set; }

        public DatiCsvCompilati() // Costruttore per inizializzare le liste
        {
            Dichiaranti = new List<Dichiarante>();
            UtenzeIdriche = new List<UtenzaIdrica>();
            Atti = new List<Atto.Atto>();
            reports = new List<Report>();
        }
    }
}