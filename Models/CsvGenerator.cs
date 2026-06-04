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
    private static string EscapeCsvField(string? field, string delimiter)
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
                var esito = NormalizzaEsitoBonus(domanda.esito);
                var codiceFornitura = string.Empty;
                var codiceFiscale = string.Empty;

                if (esito == "01")
                {
                    codiceFornitura = EstraiCodiceFornitura(domanda);
                    codiceFiscale = !string.IsNullOrWhiteSpace(domanda.codiceFiscaleUtenzaTrovata)
                        ? domanda.codiceFiscaleUtenzaTrovata.Trim()
                        : domanda.codiceFiscaleRichiedente?.Trim() ?? string.Empty;
                }
                else if (esito == "02")
                {
                    codiceFiscale = domanda.codiceFiscaleRichiedente?.Trim() ?? string.Empty;
                }

                var campi = new List<string>
                {
                    EscapeCsvField(domanda.codiceBonus?.Trim() ?? string.Empty, Delimitatore),
                    EscapeCsvField(esito, Delimitatore),
                    EscapeCsvField(codiceFornitura, Delimitatore),
                    EscapeCsvField(codiceFiscale, Delimitatore),
                    EscapeCsvField(domanda.numeroComponenti.ToString(), Delimitatore)
                };

                csvContent.AppendLine(string.Join(Delimitatore, campi));
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
                var campi = new List<string>
                {
                    EscapeCsvField(domanda.codiceBonus?.Trim() ?? string.Empty, Delimitatore),
                    EscapeCsvField(NormalizzaEsitoCompetenzaTerritoriale(domanda.esitoStr), Delimitatore)
                };

                csvContent.AppendLine(string.Join(Delimitatore, campi));
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }

    public static byte[] GeneraCsvAnagrafe(List<Dichiarante> dati)
    {
        StringBuilder csvContent = new StringBuilder();
        List<string> headers = new List<string>
        {
            "ID",
            "COGNOME",
            "NOME",
            "CODICE_FISCALE",
            "SESSO",
            "DATA_NASCITA",
            "COMUNE_NASCITA",
            "INDIRIZZO_RESIDENZA",
            "NUMERO_CIVICO",
            "CODICE_ABITANTE",
            "CODICE_FAMIGLIA",
            "PARENTELA",
            "CF_INTESTATARIO_SCHEDA",
            "NUMERO_COMPONENTI",
            "ID_ENTE",
            "ID_USER",
            "DATA_CREAZIONE",
            "DATA_AGGIORNAMENTO",
            "DATA_CANCELLAZIONE"
        };
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any())
        {
            foreach (var dichiarante in dati)
            {
                var campi = new List<string>
                {
                    EscapeCsvField(dichiarante.id?.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(dichiarante.Cognome, Delimitatore),
                    EscapeCsvField(dichiarante.Nome, Delimitatore),
                    EscapeCsvField(dichiarante.CodiceFiscale, Delimitatore),
                    EscapeCsvField(dichiarante.Sesso, Delimitatore),
                    EscapeCsvField(FormattaData(dichiarante.DataNascita), Delimitatore),
                    EscapeCsvField(dichiarante.ComuneNascita, Delimitatore),
                    EscapeCsvField(dichiarante.IndirizzoResidenza, Delimitatore),
                    EscapeCsvField(dichiarante.NumeroCivico, Delimitatore),
                    EscapeCsvField(dichiarante.CodiceAbitante?.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(dichiarante.CodiceFamiglia?.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(dichiarante.Parentela, Delimitatore),
                    EscapeCsvField(dichiarante.CodiceFiscaleIntestatarioScheda, Delimitatore),
                    EscapeCsvField(dichiarante.NumeroComponenti.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(dichiarante.IdEnte.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(dichiarante.IdUser.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(FormattaDataOra(dichiarante.data_creazione), Delimitatore),
                    EscapeCsvField(FormattaDataOra(dichiarante.data_aggiornamento), Delimitatore),
                    EscapeCsvField(FormattaDataOra(dichiarante.data_cancellazione), Delimitatore)
                };

                csvContent.AppendLine(string.Join(Delimitatore, campi));
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }

    public static byte[] GeneraCsvUtenzeIdriche(List<UtenzaIdrica> dati)
    {
        StringBuilder csvContent = new StringBuilder();
        List<string> headers = new List<string>
        {
            "ID",
            "ID_ACQUEDOTTO",
            "STATO",
            "PERIODO_INIZIALE",
            "PERIODO_FINALE",
            "MATRICOLA_CONTATORE",
            "INDIRIZZO_UBICAZIONE",
            "NUMERO_CIVICO",
            "SUB_UBICAZIONE",
            "SCALA_UBICAZIONE",
            "PIANO",
            "INTERNO",
            "TIPO_UTENZA",
            "COGNOME",
            "NOME",
            "SESSO",
            "DATA_NASCITA",
            "CODICE_FISCALE",
            "PARTITA_IVA",
            "ID_DICHIARANTE",
            "DATA_CREAZIONE",
            "DATA_AGGIORNAMENTO",
            "ID_ENTE",
            "ID_USER",
            "ID_TOPONIMO"
        };
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any())
        {
            foreach (var utenza in dati)
            {
                var campi = new List<string>
                {
                    EscapeCsvField(utenza.id.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(utenza.idAcquedotto, Delimitatore),
                    EscapeCsvField(utenza.stato?.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(FormattaData(utenza.periodoIniziale), Delimitatore),
                    EscapeCsvField(FormattaData(utenza.periodoFinale), Delimitatore),
                    EscapeCsvField(utenza.matricolaContatore, Delimitatore),
                    EscapeCsvField(utenza.indirizzoUbicazione, Delimitatore),
                    EscapeCsvField(utenza.numeroCivico, Delimitatore),
                    EscapeCsvField(utenza.subUbicazione, Delimitatore),
                    EscapeCsvField(utenza.scalaUbicazione, Delimitatore),
                    EscapeCsvField(utenza.piano, Delimitatore),
                    EscapeCsvField(utenza.interno, Delimitatore),
                    EscapeCsvField(utenza.tipoUtenza, Delimitatore),
                    EscapeCsvField(utenza.cognome, Delimitatore),
                    EscapeCsvField(utenza.nome, Delimitatore),
                    EscapeCsvField(utenza.sesso, Delimitatore),
                    EscapeCsvField(FormattaData(utenza.DataNascita), Delimitatore),
                    EscapeCsvField(utenza.codiceFiscale, Delimitatore),
                    EscapeCsvField(utenza.partitaIva, Delimitatore),
                    EscapeCsvField(utenza.IdDichiarante?.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(FormattaDataOra(utenza.data_creazione), Delimitatore),
                    EscapeCsvField(FormattaDataOra(utenza.data_aggiornamento), Delimitatore),
                    EscapeCsvField(utenza.IdEnte.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(utenza.IdUser.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(utenza.idToponimo?.ToString(CultureInfo.InvariantCulture), Delimitatore)
                };

                csvContent.AppendLine(string.Join(Delimitatore, campi));
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }

    public static byte[] GeneraCsvToponomi(List<Toponimo> dati)
    {
        StringBuilder csvContent = new StringBuilder();
        List<string> headers = new List<string>
        {
            "ID",
            "DENOMINAZIONE",
            "NORMALIZZAZIONE",
            "TIPO_TOPONIMO",
            "INTESTAZIONE",
            "INTESTAZIONE_NORMALIZZATA",
            "DATA_CREAZIONE",
            "DATA_AGGIORNAMENTO",
            "ID_ENTE"
        };
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any())
        {
            foreach (var toponimo in dati)
            {
                var campi = new List<string>
                {
                    EscapeCsvField(toponimo.id?.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(toponimo.denominazione, Delimitatore),
                    EscapeCsvField(toponimo.normalizzazione, Delimitatore),
                    EscapeCsvField(toponimo.tipoToponimo, Delimitatore),
                    EscapeCsvField(toponimo.intestazione, Delimitatore),
                    EscapeCsvField(toponimo.intestazioneNormalizzata, Delimitatore),
                    EscapeCsvField(FormattaDataOra(toponimo.dataCreazione), Delimitatore),
                    EscapeCsvField(FormattaDataOra(toponimo.dataAggiornamento), Delimitatore),
                    EscapeCsvField(toponimo.IdEnte.ToString(CultureInfo.InvariantCulture), Delimitatore)
                };

                csvContent.AppendLine(string.Join(Delimitatore, campi));
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }

    public static byte[] GeneraCsvDomandeIncongruenti(List<Domanda> dati)
    {
        StringBuilder csvContent = new StringBuilder();
        List<string> headers = new List<string>
        {
            "ID",
            "ID_ATO",
            "COD_BONUS_IDRICO",
            "ESITO_STR",
            "ESITO",
            "CODICE_FISCALE_RICHIEDENTE",
            "CODICE_FISCALE_UTENZA_TROVATA",
            "ID_DICHIARANTE",
            "ID_UTENZA",
            "ID_FORNITURA",
            "NOME_DICHIARANTE",
            "COGNOME_DICHIARANTE",
            "ANNO_VALIDITA",
            "DATA_INIZIO_VALIDITA",
            "DATA_FINE_VALIDITA",
            "INDIRIZZO_ABITAZIONE",
            "NUMERO_CIVICO",
            "ISTAT",
            "CAP_ABITAZIONE",
            "PROVINCIA_ABITAZIONE",
            "PRESENZA_POD",
            "NUMERO_COMPONENTI",
            "MC",
            "INCONGRUENZE",
            "NOTE"
        };
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any())
        {
            foreach (var domanda in dati)
            {
                var campi = new List<string>
                {
                    EscapeCsvField(domanda.id.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(domanda.idAto, Delimitatore),
                    EscapeCsvField(domanda.codiceBonus, Delimitatore),
                    EscapeCsvField(domanda.esitoStr, Delimitatore),
                    EscapeCsvField(domanda.esito, Delimitatore),
                    EscapeCsvField(domanda.codiceFiscaleRichiedente, Delimitatore),
                    EscapeCsvField(domanda.codiceFiscaleUtenzaTrovata, Delimitatore),
                    EscapeCsvField(domanda.idDichiarante?.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(domanda.idUtenza?.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(domanda.idFornitura?.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(domanda.nomeDichiarante, Delimitatore),
                    EscapeCsvField(domanda.cognomeDichiarante, Delimitatore),
                    EscapeCsvField(domanda.annoValidita, Delimitatore),
                    EscapeCsvField(domanda.dataInizioValidita.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(domanda.dataFineValidita.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(domanda.indirizzoAbitazione, Delimitatore),
                    EscapeCsvField(domanda.numeroCivico, Delimitatore),
                    EscapeCsvField(domanda.istat, Delimitatore),
                    EscapeCsvField(domanda.capAbitazione, Delimitatore),
                    EscapeCsvField(domanda.provinciaAbitazione, Delimitatore),
                    EscapeCsvField(domanda.presenzaPod, Delimitatore),
                    EscapeCsvField(domanda.numeroComponenti?.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(domanda.mc?.ToString(CultureInfo.InvariantCulture), Delimitatore),
                    EscapeCsvField(domanda.incongruenze?.ToString() ?? string.Empty, Delimitatore),
                    EscapeCsvField(domanda.note, Delimitatore)
                };

                csvContent.AppendLine(string.Join(Delimitatore, campi));
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }

    private static string NormalizzaEsitoBonus(string? esito)
    {
        var valore = esito?.Trim() ?? string.Empty;

        if (int.TryParse(valore, out var esitoNumerico) && esitoNumerico >= 1 && esitoNumerico <= 4)
        {
            return esitoNumerico.ToString("00", CultureInfo.InvariantCulture);
        }

        return valore switch
        {
            "01" or "02" or "03" or "04" => valore,
            _ => string.Empty
        };
    }

    private static string NormalizzaEsitoCompetenzaTerritoriale(string? esito)
    {
        var valore = esito?.Trim().ToUpperInvariant();
        return valore == "SI" || valore == "NO" ? valore : "NO";
    }

    private static string FormattaData(DateTime? data)
    {
        return data?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string FormattaDataOra(DateTime? data)
    {
        return data?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty;
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
                riga.Append(EscapeCsvField(EstraiCodiceFornitura(domanda), Delimitatore)).Append(Delimitatore);
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
        List<string> headers = new List<string> { "ID", "ID_ATO", "Codice Bonus", "ID_Fornitura", "Esito STR", "Esito", "Codice Fiscale Richiedente", "Codice Fiscale x bonus", "Id utenza", "Nome Dichiarante", "Cognome Dichiarante", "ID Dichiarante", "Anno Validità", "Indirizzo Abitazione", "Numero civico", "Istat", "CAP", "PROVINCIA", "INIZIO VALIDITA", "FINE VALIDITA", "PRESENZA POD", "SERIE", "MC", "Incongruenze", "Note", "Numero Componenti", "Data Aggiornamento", "UsaSnapshotUtenze", "FonteFornitura", "CodiceFornituraUsato", "IdUtenzaSnapshot" };
        csvContent.AppendLine(string.Join(Delimitatore, headers));

        if (dati != null && dati.Any())
        {
            foreach (var domanda in dati)
            {
                StringBuilder riga = new StringBuilder();
                riga.Append(EscapeCsvField(domanda.id.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.idAto.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(domanda.codiceBonus.ToString(), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(EstraiCodiceFornitura(domanda), Delimitatore)).Append(Delimitatore);
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
                riga.Append(EscapeCsvField(domanda.note?.Contains("FonteFornitura=SNAPSHOT_UTENZE") == true ? "true" : "false", Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(EstraiValoreNota(domanda.note, "FonteFornitura"), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(EstraiCodiceFornitura(domanda), Delimitatore)).Append(Delimitatore);
                riga.Append(EscapeCsvField(EstraiValoreNota(domanda.note, "IdUtenzaSnapshot"), Delimitatore)).Append(Delimitatore);
                csvContent.AppendLine(riga.ToString());
            }
        }

        return Encoding.UTF8.GetBytes(csvContent.ToString());
    }

    private static string EstraiCodiceFornitura(Domanda domanda)
    {
        var codiceDaNote = EstraiValoreNota(domanda.note, "CodiceFornituraUsato");
        return !string.IsNullOrWhiteSpace(codiceDaNote)
            ? codiceDaNote
            : domanda.idFornitura?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string EstraiValoreNota(string? note, string chiave)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return string.Empty;
        }

        var marker = chiave + "=";
        var start = note.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return string.Empty;
        }

        start += marker.Length;
        var end = note.IndexOfAny(new[] { ';', '\n', '\r', '.' }, start);
        return end < 0 ? note[start..].Trim() : note[start..end].Trim();
    }
}
