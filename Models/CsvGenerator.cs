using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using Models;
using System.Globalization; 
public static class CsvGenerator
{
    private const string Delimitatore = ";";

    /*
        Questa classe contiene tre funzioni statiche per generare file CSV da liste di oggetti domanda.
        Ogni funzione crea un CSV con intestazioni e campi specifici, gestendo l'escape dei campi che contengono caratteri speciali.
        Le funzioni restituiscono il contenuto del CSV come array di byte codificato in UTF-8.
    */

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
    
    // Funzione 1: consente di generare il file x Bonus Idrico

    public static byte[] GeneraCsvBonusIdrico(List<Domanda> dati)
    {
        StringBuilder csvContent = new StringBuilder();
        List<string> headers = new List<string> { "COD_BONUS_IDRICO", "ESITO", "COD_FORNITURA", "CF", "N_NUCLEO" };
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any()) 
        {
            foreach (var domanda in dati)
            {
                StringBuilder riga = new StringBuilder();
                var codiceFornitua = domanda.idFornitura.ToString();

                riga.Append(EscapeCsvField(domanda.codiceBonus, Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.esito, Delimitatore)).Append(Delimitatore);

                if (!string.IsNullOrEmpty(codiceFornitua) && domanda.esito == "01")
                {
                    riga.Append(EscapeCsvField(codiceFornitua, Delimitatore)).Append(Delimitatore);
                }
                else
                {
                    riga.Append(EscapeCsvField("", Delimitatore)).Append(Delimitatore);
                }
                if (!string.IsNullOrEmpty(domanda.codiceFiscaleRichiedente) && (domanda.esito == "01" || domanda.esito == "02"))
                {
                    riga.Append(EscapeCsvField(domanda.codiceFiscaleRichiedente.ToString(), Delimitatore)).Append(Delimitatore);
                }
                else
                {
                    riga.Append(EscapeCsvField("", Delimitatore)).Append(Delimitatore);
                }

                riga.Append(EscapeCsvField(domanda.numeroComponenti.ToString() ?? "N/D", Delimitatore)).Append(Delimitatore);

                // Usa CultureInfo.InvariantCulture per formattare i decimali con il punto
                csvContent.AppendLine(riga.ToString());
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }

    // Funzione 2: consente di generare il file x Competenza Territoriale
    public static byte[] GeneraCsvCompetenzaTerritoriale(List<Domanda> dati)
    {
        StringBuilder csvContent = new StringBuilder();
        List<string> headers = new List<string> { "ID_RICHIESTA", "ESITO" };
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any())
        {
            foreach (var domanda in dati)
            {
                StringBuilder riga = new StringBuilder();
                riga.Append(EscapeCsvField(domanda.codiceBonus.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.esitoStr, Delimitatore)).Append(Delimitatore);
                csvContent.AppendLine(riga.ToString());
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }

    // Funzione 3: consente di generare il file x Siscom

    public static byte[] GeneraCsvSiscom(List<Domanda> dati, int serie)
    {
        StringBuilder csvContent = new StringBuilder();
        List<string> headers = new List<string> { "ID_ACQUEDOTTO", "ANNO","SERIE","NUMERO_COMPONENTI","MC"};
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any())
        {
            foreach (var domanda in dati)
            {
                StringBuilder riga = new StringBuilder();
                riga.Append(EscapeCsvField(domanda.idFornitura.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.annoValidita.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(serie.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.numeroComponenti.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                 riga.Append(EscapeCsvField(domanda.mc.ToString() ?? "", Delimitatore)).Append(Delimitatore);      
                csvContent.AppendLine(riga.ToString());
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }

    // Funzione 4: consente di generare il file di Debug 
    // Contiene tutti i campi del domanda
    // Utile per analisi e debug

     public static byte[] GeneraCsvDebug(List<Domanda> dati, int serie)
    {
        StringBuilder csvContent = new StringBuilder();
        List<string> headers = new List<string> { "ID", "ID_ATO", "Codice Bonus", "ID_Fornitura", "Esito STR", "Esito", "Codice Fiscale Richiedente", "Codice Fiscale x bonus", "Id utenza", "Nome Dichiarante", "Cognome Dichiarante", "ID Dichiarante", "Anno Validità", "Indirizzo Abitazione", "Numero civico", "Istat", "CAP", "PROVINCIA", "INIZIO VALIDITA", "FINE VALIDITA", "PRESENZA POD", "SERIE", "MC", "Incongruenze", "Note", "Numero Componenti", "Data Aggiornamento" };
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any())
        {
            foreach (var domanda in dati)
            {
                StringBuilder riga = new StringBuilder();
                riga.Append(EscapeCsvField(domanda.id.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.idAto.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.codiceBonus.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.idFornitura.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.esitoStr.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.esito.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.codiceFiscaleRichiedente.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.codiceFiscaleUtenzaTrovata?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.idUtenza?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.nomeDichiarante.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.cognomeDichiarante.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.idDichiarante?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.annoValidita.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.indirizzoAbitazione.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.numeroCivico?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.istat.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.capAbitazione.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.provinciaAbitazione?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.dataInizioValidita.ToString("yyyy-MM-dd"), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.dataFineValidita.ToString("yyyy-MM-dd"), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.presenzaPod.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(serie.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.mc?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.incongruenze?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.note?.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.numeroComponenti.ToString() ?? "", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.DataAggiornamento?.ToString("yyyy-MM-dd HH:mm:ss") ?? "", Delimitatore)).Append(Delimitatore);
                csvContent.AppendLine(riga.ToString());
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }
}