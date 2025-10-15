using System.Collections.Generic;
using Models;

namespace leggiCSV
{
    public class DatiCsvCompilati // Nome più descrittivo per il contenitore
    {
        // Dichirazione delle voci
        public List<Dichiarante> Dichiaranti { get; set; }

        public List<Dichiarante> DichiarantiDaAggiornare { get; set; }

        public List<UtenzaIdrica> UtenzeIdriche { get; set; }

        public List<UtenzaIdrica> UtenzeIdricheEsistente { get; set; }

        public int? countIndirizziMalFormati { get; set; }
        
        public List<Domanda> domande { get; set; }

        public List<Domanda> domandeDaAggiornare { get; set; }

        public List<Toponimo> Toponimi { get; set; }

        public List<Toponimo> ToponimiDaAggiornare { get; set; }

        public List<Report> Reports {get;set;}

        public DatiCsvCompilati() // Costruttore per inizializzare le liste
        {
            Dichiaranti = new List<Dichiarante>();

            DichiarantiDaAggiornare = new List<Dichiarante>();

            UtenzeIdriche = new List<UtenzaIdrica>();

            UtenzeIdricheEsistente = new List<UtenzaIdrica>();

            domande = new List<Domanda>();

            domandeDaAggiornare = new List<Domanda>();

            Toponimi = new List<Toponimo>();

            ToponimiDaAggiornare = new List<Toponimo>();

            Reports = new List<Report>();

            countIndirizziMalFormati = null;
        }
    }
}