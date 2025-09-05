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
                if (!string.IsNullOrEmpty(report.codiceFiscaleRichiedente) && (report.esito == "01" || report.esito == "02"))
                {
                    riga.Append(EscapeCsvField(report.codiceFiscaleRichiedente.ToString(), Delimitatore)).Append(Delimitatore);
                }else
                {
                    riga.Append(EscapeCsvField("", Delimitatore)).Append(Delimitatore);
                }
                                
                riga.Append(EscapeCsvField(report.numeroComponenti.ToString() ?? "N/D", Delimitatore)).Append(Delimitatore);
                
                // Usa CultureInfo.InvariantCulture per formattare i decimali con il punto
                csvContent.AppendLine(riga.ToString());
            }
        }
       
        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }

   
    public static byte[] GeneraCsvCompetenzaTerritoriale(List<Report> dati) 
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

    public static byte[] GeneraCsvSiscom(List<Report> dati)
    {
        StringBuilder csvContent = new StringBuilder();
        List<string> headers = new List<string> { "ID_ACQUEDOTTO", "ANNO","SERIE","NUMERO_COMPONENTI","MC"};
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any())
        {
            foreach (var report in dati)
            {
                StringBuilder riga = new StringBuilder();
                riga.Append(EscapeCsvField(report.idFornitura.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.annoValidita.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.serie.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.numeroComponenti.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                 riga.Append(EscapeCsvField(report.mc.ToString() ?? "", Delimitatore)).Append(Delimitatore);      
                csvContent.AppendLine(riga.ToString());
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }

     public static byte[] GeneraCsvDebug(List<Report> dati)
    {
        StringBuilder csvContent = new StringBuilder();
        List<string> headers = new List<string> { "ID", "ID_ATO", "Codice Bonus", "ID_Fornitura", "Esito STR", "Esito", "Codice Fiscale Richiedente", "Codice Fiscale x bonus", "Id utenza", "Nome Dichiarante", "Cognome Dichiarante","ID Dichiarante", "Anno Validità", "Indirizzo Abitazione", "Numero civico", "Istat", "CAP", "PROVINCIA", "INIZIO VALIDITA", "FINE VALIDITA", "PRESENZA POD", "SERIE","MC", "Incongruenze", "Note", "Numero Componenti", "Data Creazione", "Data Aggiornamento", "IdEnte", "IdUser" };
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any())
        {
            foreach (var report in dati)
            {
                StringBuilder riga = new StringBuilder();
                riga.Append(EscapeCsvField(report.id.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.idAto.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.codiceBonus.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.idFornitura.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.esitoStr.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.esito.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.codiceFiscaleRichiedente.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.codiceFiscaleUtenzaTrovata?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.idUtenza?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.nomeDichiarante.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.cognomeDichiarante.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.idDichiarante?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.annoValidita.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.indirizzoAbitazione.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.numeroCivico?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.istat.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.capAbitazione.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.provinciaAbitazione?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.dataInizioValidita.ToString("yyyy-MM-dd"), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.dataFineValidita.ToString("yyyy-MM-dd"), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.presenzaPod.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.serie.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.mc?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.incongruenze?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.note?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.numeroComponenti.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.DataCreazione.ToString("yyyy-MM-dd HH:mm:ss"), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.DataAggiornamento?.ToString("yyyy-MM-dd HH:mm:ss") ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.IdEnte.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(report.IdUser.ToString(), Delimitatore)).Append(Delimitatore);                
                csvContent.AppendLine(riga.ToString());
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }
}