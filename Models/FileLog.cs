using System;
using System.IO;

namespace BonusIdrici2.Models{
    
    /*
        Questa classe gestisce il logging delle operazioni in un file di log.
        Viene utilizzata per tracciare eventi informativi, avvisi e errori durante l'esecuzione dell'applicazione.

        METODI:
        - LogInfo: registra un messaggio informativo.
        - LogWarning: registra un messaggio di avviso.
        - LogError: registra un messaggio di errore.
        - LogDebug: registra un messaggio di debug (utile durante lo sviluppo).

        Il file di log viene creato se non esiste e ogni messaggio viene timestampato per facilitare il tracciamento degli eventi.

    */
    public class FileLog
    {
        private readonly string logFilePath;
        private readonly object lockObj = new();

        // Costruttore che accetta il percorso del file di log
        public FileLog(string filePath)
        {
            logFilePath = filePath;

            // Crea il file se non esiste
            if (!File.Exists(logFilePath))
            {
                using var stream = File.Create(logFilePath);
            }
        }

        // Metodi per il logging di diversi livelli di messaggi
        public void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogWarning(string message)
        {
            WriteLog("WARNING", message);
        }

        public void LogError(string message)
        {
            WriteLog("ERROR", message);
        }

        public void LogDebug(string message)
        {
            WriteLog("DEBUG", message);
        }

        // Metodo privato per scrivere il log nel file

        private void WriteLog(string level, string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

            lock (lockObj) // Protezione per ambienti multithread
            {
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            }
        }
    }

}

