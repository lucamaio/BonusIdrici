using System;
using System.IO;

namespace BonusIdrici2.Models{
    public class FileLog
    {
        private readonly string logFilePath;
        private readonly object lockObj = new();

        public FileLog(string filePath)
        {
            logFilePath = filePath;

            // Crea il file se non esiste
            if (!File.Exists(logFilePath))
            {
                using var stream = File.Create(logFilePath);
            }
        }

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

