using System;

namespace Models.ViewModels
{  
    public class LogViewModel
    {
        public DateTime Timestamp { get; set; }
        public string TipoLog { get; set; }
        public string Messaggio { get; set; }

        // Costruttore 
        public LogViewModel(DateTime timestamp, string tipoLog, string messaggio)
        {
            Timestamp = timestamp;
            TipoLog = tipoLog;
            Messaggio = messaggio;
        }

        // Costruttore vuoto
        public LogViewModel()
        {
            Timestamp = DateTime.Now;
            TipoLog = string.Empty;
            Messaggio = string.Empty;
        }

        // Override ToString() per una rappresentazione leggibile
        public override string ToString()
        {
            return $"{Timestamp} [{TipoLog}]\n{Messaggio}";
        }
    }
}