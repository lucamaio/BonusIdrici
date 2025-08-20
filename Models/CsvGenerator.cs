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
                
                if (!string.IsNullOrEmpty(codiceFornitua) && report.esito == "01")
                {
                    riga.Append(EscapeCsvField(codiceFornitua, Delimitatore)).Append(Delimitatore);
                }
                else
                {
                    riga.Append(EscapeCsvField("", Delimitatore)).Append(Delimitatore);
                }
                if (!string.IsNullOrEmpty(report.codiceFiscale) && (report.esito == "01" || report.esito == "02"))
                {
                    riga.Append(EscapeCsvField(report.codiceFiscale.ToString(), Delimitatore)).Append(Delimitatore);
                }else
                {
                    riga.Append(EscapeCsvField("", Delimitatore)).Append(Delimitatore);
                }
                                
                riga.Append(EscapeCsvField(report.numeroComponenti.ToString(), Delimitatore)).Append(Delimitatore);
                
                // Usa CultureInfo.InvariantCulture per formattare i decimali con il punto
                // riga.Append(EscapeCsvField(report.codiceFiscale.ToString(CultureInfo.InvariantCulture), Delimitatore));
                csvContent.AppendLine(riga.ToString());
            }
        }
       
        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }

   
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

    // Funzione 3: consente di generare il file x Siscom

    public static byte[] GeneraCsvSiscom(List<Report> dati) // Nota: nome corretto "Territoriale"
    {
        StringBuilder csvContent = new StringBuilder();
        List<string> headers = new List<string> { "ID_ACQUEDOTTO", "ANNO","SERIE","COMPONENTI","MC"};
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any())
        {
            foreach (var report in dati)
            {
                StringBuilder riga = new StringBuilder();
                riga.Append(EscapeCsvField(report.idFornitura.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.annoValidita.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.serie.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.numeroComponenti.ToString(), Delimitatore)).Append(Delimitatore);
                 riga.Append(EscapeCsvField(report.mc.ToString(), Delimitatore)).Append(Delimitatore);      
                csvContent.AppendLine(riga.ToString());
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }
}