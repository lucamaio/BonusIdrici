using System.ComponentModel.DataAnnotations;

namespace Models
{
    /*
        Questa classe rappresenta una singola voce di log.
        Viene utilizzata per memorizzare informazioni sui log letti da un file di log.

        PROPRIETA':
        - Id: identificatore univoco della voce di log.
        - Timestamp: data e ora in cui è stato registrato il log.
        - TipoLog: tipo di log (es. INFO, WARNING, ERROR, DEBUG).
        - Messaggio: messaggio associato al log.

        COSTRUTTORE:
        - Log(string riga): accetta una riga di testo dal file di log e la suddivide nei campi appropriati.

    */
    public class Log
    {
        public DateTime Timestamp { get; set; }
        public string TipoLog { get; set; }
        public string Messaggio { get; set; }

        public Log()
        {
            Timestamp = DateTime.Now;
            TipoLog = string.Empty;
            Messaggio = string.Empty;
        }

        // Costruttore
        public Log(string riga)
        {
            if (string.IsNullOrWhiteSpace(riga))
            {
                throw new ArgumentException("La riga del log non può essere vuota.");
            }

            var campi = riga.Split('['); // Mi ricavo la data

            if (campi.Length < 2)
            {
                throw new ArgumentException("La riga del log non è nel formato corretto.");
            }

            // Ora setto il timestamp
            Timestamp = campi[0].Trim().Length >= 19 ? DateTime.Parse(campi[0].Trim()) : throw new ArgumentException("La riga del log non è nel formato corretto.");

            // Adesso mi ricavo il tipo di log e il messaggio
            var TipoLogEMessaggio = campi[1].Split(']');
            TipoLog = TipoLogEMessaggio[0].Trim().Length > 3 ? TipoLogEMessaggio[0].Trim() : throw new ArgumentException("La riga del log non è nel formato corretto.");

            // Adesso setto il messaggio
            Messaggio = TipoLogEMessaggio[1].Trim().Length > 0 ? TipoLogEMessaggio[1].Trim().ToString() : throw new ArgumentException("La riga del log non è nel formato corretto.");

        }

        // Funzione che mi restituisce il messaggio in breve

        public string GetShortMessage(int maxLength = 50)
        {
            if (Messaggio.Length <= maxLength)
            {
                return Messaggio;
            }
            return Messaggio.Substring(0, maxLength) + "...";
        }

        // Override del metodo ToString per una rappresentazione leggibile del log
        public override string ToString()
        {
            return $"{Timestamp} [{TipoLog}]\n{Messaggio}";
        }

    }


}   