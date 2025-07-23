using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using BonusIdrici2.Models;
using System.Globalization; 
public static class CsvGenerator
{
    private const string Delimitatore = ";";

    // Funzione helper per l'escape dei campi CSV (mantienila invariata)
    private static string EscapeCsvField(string field, string delimiter)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        if (field.Contains(delimiter) || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
        {
            field = field.Replace("\"", "\"\"");
            return $"\"{field}\"";
        }
        return field;
    }

    public static byte[] GeneraCsvBonusIdrico(List<Report> dati)
    {
        StringBuilder csvContent = new StringBuilder();
        List<string> headers = new List<string> { "COD_BONUS_IDRICO", "ESITO", "COD_FORNITURA", "CF","N_NUCLEO" };
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any()) // Usa .Any() per controllare se la lista contiene elementi
        {
            foreach (var report in dati)
            {
                StringBuilder riga = new StringBuilder();
                var codiceFornitua = report.idFornitura.ToString();
                
                riga.Append(EscapeCsvField(report.codiceBonus, Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.esito, Delimitatore)).Append(Delimitatore);
                if (!string.IsNullOrEmpty(codiceFornitua))
                {
                    riga.Append(EscapeCsvField(codiceFornitua, Delimitatore)).Append(Delimitatore);
                }
                else
                {
                    riga.Append(EscapeCsvField("", Delimitatore)).Append(Delimitatore);
                }
                riga.Append(EscapeCsvField(report.codiceFiscale.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.numeroComponenti.ToString(), Delimitatore)).Append(Delimitatore);
                
                // Usa CultureInfo.InvariantCulture per formattare i decimali con il punto
                // riga.Append(EscapeCsvField(report.codiceFiscale.ToString(CultureInfo.InvariantCulture), Delimitatore));
                csvContent.AppendLine(riga.ToString());
            }
        }
        else
        {
            // Se non ci sono dati, potresti voler aggiungere una riga vuota o un messaggio,
            // oppure semplicemente restituire l'intestazione. Dipende dalla tua specifica.
            // Per ora, restituisce solo l'intestazione se dati è null o vuoto.
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }

    /// <summary>
    /// Genera il contenuto CSV per il report "Esito Competenza Territoriale".
    /// </summary>
    /// <param name="dati">Lista di oggetti Report contenente i dati della competenza territoriale.</param>
    /// <returns>Array di byte del contenuto CSV.</returns>
    public static byte[] GeneraCsvCompetenzaTerritoriale(List<Report> dati) // Nota: nome corretto "Territoriale"
    {
        StringBuilder csvContent = new StringBuilder();
        List<string> headers = new List<string> { "ID_RICHIESTA", "ESITO" };
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any())
        {
            foreach (var report in dati)
            {
                StringBuilder riga = new StringBuilder();
                riga.Append(EscapeCsvField(report.codiceBonus.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.esitoStr, Delimitatore)).Append(Delimitatore);
                csvContent.AppendLine(riga.ToString());
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }
}