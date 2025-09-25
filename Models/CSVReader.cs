using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BonusIdrici2.Data;
using BonusIdrici2.Controllers;

using leggiCSV;
using Org.BouncyCastle.Crypto.Digests;
using ZstdSharp.Unsafe;
using Org.BouncyCastle.Bcpg;
using BonusIdrici2.Models;
using Mysqlx.Notice;
using System.Linq.Expressions;

public class CSVReader
{
    private const char CsvDelimiter = ';';  // Delimitatore standard per i file CSV

    /*
        Questa classe contiene le funzioni per la lettura dei file CSV
        - LoadAnagrafe: legge il file CSV dell'anagrafe e restituisce una lista di dichiaranti da aggiungere o aggiornare
        - LeggiFileUtenzeIdriche: legge il file CSV delle utenze idriche e restituisce una lista di utenze da aggiungere o aggiornare
        - LeggiFileBonusIdrico: legge il file CSV dei bonus idrici e restituisce una lista di bonus da aggiungere o aggiornare

        Ogni funzione effettua le seguenti operazioni:
        1. Apre il file CSV e legge le righe
        2. Per ogni riga, effettua delle verifiche preliminari sui campi
        3. Se i campi sono validi, crea un'istanza dell'oggetto corrispondente (Dichiarante, UtenzaIdrica, BonusIdrico)
        4. Aggiunge l'istanza alla lista dei dati da aggiungere o aggiornare
        5. Restituisce la lista dei dati da aggiungere o aggiornare

        Ogni funzione utilizza un file di log per registrare le operazioni effettuate e gli eventuali errori riscontrati
    */

    // Funzione 1: Carica il file CSV dell'anagrafe e restituisce una lista di dichiaranti da aggiungere o aggiornare
    public static DatiCsvCompilati LoadAnagrafe(string percorsoFile, int selectedEnteId, ApplicationDbContext _context, int idUser)
    {
        // Parte 1: Creazione della variabile da restituire e apertura file di log
        var datiComplessivi = new DatiCsvCompilati();
        FileLog logFile = new FileLog($"wwwroot/log/Elaborazione_Anagrafe.log");
        List<string> errori = new List<string>();
        try
        {
            // Leggo il numero righe da processare escludendo l'intestazione
            var righe = File.ReadAllLines(percorsoFile).Skip(1);
            int rigaCorrente = 1;
            logFile.LogInfo($"Nuovo caricamento dati Anagrafe ID ENTE: {selectedEnteId}");
            logFile.LogInfo($"Numero di righe da caricare: {righe.Count()}");

            // Inizio - Lettura delle varie righe
            foreach (var riga in righe)
            {
                rigaCorrente++;
                var error = false;

                // Parte 2: Verifiche Preliminari sui campi

                // a) verifico se la riga è vuota
                if (string.IsNullOrWhiteSpace(riga)) continue;

                var campi = riga.Split(CsvDelimiter);

                // b) Verifico che il file contine tutti i vampi minimi per poter procedere

                if (campi.Length < 16)
                {
                    errori.Add($"Attenzione: Riga {rigaCorrente} malformata, saltata. Numero di campi: {campi.Length}. Attesi almeno 16.");
                    continue;
                }

                // c) Verifico che il campo cognome è valido 

                if (string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[0])))
                {
                    errori.Add($"Attenzione: Cognome mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // d) Verifico che il campo nome è valido

                if (string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[1])))
                {
                    errori.Add($"Attenzione: Nome mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // e) Verifico che il Codice Fiscale è valido

                if (string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[2])) || FunzioniTrasversali.rimuoviVirgolette(campi[2]).Length != 16)
                {
                    errori.Add($"Attenzione: Codice Fiscale mancante o mal formato, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // f) Verifico il campo sesso se è valido

                if (string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[3])) && (FunzioniTrasversali.rimuoviVirgolette(campi[3]).ToUpper() != "M") && (FunzioniTrasversali.rimuoviVirgolette(campi[3]).ToUpper() != "F"))
                {
                    errori.Add($"Attenzione: Sesso mancante o mal formato, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // g) Verifico se il campo indirizzo residente è presente

                if (string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[7])))
                {
                    errori.Add($"Attenzione: Indirizzo residenza mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // h) Verifico se il campo numero civico è presente

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.FormattaNumeroCivico(campi[8])))
                {
                    errori.Add($"Attenzione: Numero civico mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                //  Parte 3: Controllo se sono presenti Errori

                if (error)
                {
                    continue; // Salta la riga se ci sono errori
                }

                // Creo una istanza di Dichiarante

                var dichiarante = new Dichiarante
                {
                    Cognome = FunzioniTrasversali.rimuoviVirgolette(campi[0]).ToUpper(),
                    Nome = FunzioniTrasversali.rimuoviVirgolette(campi[1]).ToUpper(),
                    CodiceFiscale = FunzioniTrasversali.rimuoviVirgolette(campi[2]).ToUpper(),
                    Sesso = FunzioniTrasversali.rimuoviVirgolette(campi[3]).ToUpper(),
                    DataNascita = FunzioniTrasversali.ConvertiData(campi[4], DateTime.MinValue),
                    ComuneNascita = FunzioniTrasversali.rimuoviVirgolette(campi[5]).ToUpper(),
                    IndirizzoResidenza = FunzioniTrasversali.rimuoviVirgolette(campi[7]).ToUpper(),
                    NumeroCivico = FunzioniTrasversali.FormattaNumeroCivico(campi[8]),
                    Parentela = FunzioniTrasversali.rimuoviVirgolette(campi[10]).ToUpper(),
                    CodiceFamiglia = int.Parse(campi[11].Trim()),
                    CodiceAbitante = int.Parse(campi[12]),
                    NumeroComponenti = int.Parse(campi[13].Trim()),
                    CodiceFiscaleIntestatarioScheda = FunzioniTrasversali.rimuoviVirgolette(campi[14]),
                    data_creazione = DateTime.Now,
                    data_cancellazione = FunzioniTrasversali.ConvertiData(campi[15]),
                    IdEnte = selectedEnteId,
                    IdUser = idUser
                };

                // Parte 4: Aggiungo il dichiarante alla lista dei dichiaranti da aggiungere se non esiste già

                var dichiaranteEsistente = _context.Dichiaranti.FirstOrDefault(d => d.CodiceFiscale == dichiarante.CodiceFiscale && d.IdEnte == selectedEnteId);

                if (dichiaranteEsistente == null)
                {
                    datiComplessivi.Dichiaranti.Add(dichiarante);
                }
                else
                {
                    // Se il dichiarante esiste già, verifico se è necessario aggiornarlo
                    bool aggiornare = false;

                    // Verifico ogni campo e aggiorno se necessario

                    // 1. Verifico il cognome

                    if (dichiaranteEsistente.Cognome != null && dichiarante.Cognome != null && dichiaranteEsistente.Cognome != dichiarante.Cognome)
                    {
                        dichiaranteEsistente.Cognome = dichiarante.Cognome;
                        aggiornare = true;
                    }
                    // 2. Verifico il nome

                    if (dichiaranteEsistente.Nome != null && dichiarante.Nome != null && dichiaranteEsistente.Nome != dichiarante.Nome)
                    {
                        dichiaranteEsistente.Nome = dichiarante.Nome;
                        aggiornare = true;
                    }

                    // 3. Verifico il sesso

                    if (dichiaranteEsistente.Sesso != null && dichiarante.Sesso != null && dichiaranteEsistente.Sesso != dichiarante.Sesso)
                    {
                        dichiaranteEsistente.Sesso = dichiarante.Sesso;
                        aggiornare = true;
                    }

                    // 4. Verifico la data di nascita

                    if (dichiaranteEsistente.DataNascita != dichiarante.DataNascita)
                    {
                        dichiaranteEsistente.DataNascita = dichiarante.DataNascita;
                        aggiornare = true;
                    }

                    // 5. Verifico il comune di nascita

                    if (dichiaranteEsistente.ComuneNascita != null && dichiarante.ComuneNascita != null && dichiaranteEsistente.ComuneNascita != dichiarante.ComuneNascita)
                    {
                        dichiaranteEsistente.ComuneNascita = dichiarante.ComuneNascita;
                        aggiornare = true;
                    }

                    // 6. Verifico l'indirizzo di residenza

                    if (dichiaranteEsistente.IndirizzoResidenza != null && dichiarante.IndirizzoResidenza != null && dichiaranteEsistente.IndirizzoResidenza != dichiarante.IndirizzoResidenza)
                    {
                        dichiaranteEsistente.IndirizzoResidenza = dichiarante.IndirizzoResidenza;
                        aggiornare = true;
                    }

                    // 7. Verifico il numero civico

                    if (dichiaranteEsistente.NumeroCivico != null && dichiarante.NumeroCivico != null && dichiaranteEsistente.NumeroCivico != dichiarante.NumeroCivico)
                    {
                        dichiaranteEsistente.NumeroCivico = dichiarante.NumeroCivico;
                        aggiornare = true;
                    }

                    // 8. Verifico la parentela

                    if (dichiaranteEsistente.Parentela != null && dichiarante.Parentela != null && dichiaranteEsistente.Parentela != dichiarante.Parentela)
                    {
                        dichiaranteEsistente.Parentela = dichiarante.Parentela;
                        aggiornare = true;
                    }

                    // 9. Verifico il codice famiglia

                    if (dichiaranteEsistente.CodiceFamiglia != dichiarante.CodiceFamiglia)
                    {
                        dichiaranteEsistente.CodiceFamiglia = dichiarante.CodiceFamiglia;
                        aggiornare = true;
                    }

                    // 10. Verifico il codice abitante

                    if (dichiaranteEsistente.CodiceAbitante != dichiarante.CodiceAbitante)
                    {
                        dichiaranteEsistente.CodiceAbitante = dichiarante.CodiceAbitante;
                        aggiornare = true;
                    }

                    // 11. Verifico il numero componenti

                    if (dichiaranteEsistente.NumeroComponenti != dichiarante.NumeroComponenti)
                    {
                        dichiaranteEsistente.NumeroComponenti = dichiarante.NumeroComponenti;
                        aggiornare = true;
                    }

                    // 12. Verifico il codice fiscale intestatario scheda

                    if (dichiaranteEsistente.CodiceFiscaleIntestatarioScheda != null && dichiarante.CodiceFiscaleIntestatarioScheda != null && dichiaranteEsistente.CodiceFiscaleIntestatarioScheda != dichiarante.CodiceFiscaleIntestatarioScheda)
                    {
                        dichiaranteEsistente.CodiceFiscaleIntestatarioScheda = dichiarante.CodiceFiscaleIntestatarioScheda;
                        aggiornare = true;
                    }

                    // 13. Verifico la data di cancellazione

                    if (dichiaranteEsistente.data_cancellazione != null && dichiarante.data_cancellazione != null && dichiaranteEsistente.data_cancellazione != dichiarante.data_cancellazione)
                    {
                        dichiaranteEsistente.data_cancellazione = dichiarante.data_cancellazione;
                        aggiornare = true;
                    }

                    // Se è necessario aggiornare il dichiarante esistente
                    if (aggiornare)
                    {
                        datiComplessivi.DichiarantiDaAggiornare.Add(dichiaranteEsistente);
                    }
                }
            }

            // Fine - Lettura delle varie righe
            // Parte 5: Stampo i messaggi di riepilogo sul file di log
            // Stampo gli errori riscontrati
            if (errori.Count > 0)
            {
                logFile.LogInfo($"Errori riscontrati {errori.Count} durante l'elaborazione:");
                foreach (var errore in errori)
                {
                    logFile.LogError(errore);
                }
            }
            else
            {
                logFile.LogInfo("Elaborazione completata senza errori.");
            }

            // Stampo il numero di dichiaranti trovati
            if (datiComplessivi.Dichiaranti.Count > 0)
            {
                logFile.LogInfo($"Trovati {datiComplessivi.Dichiaranti.Count} dichiaranti da aggiungere.");
            }
            else
            {
                logFile.LogInfo("Nessun dichiarante trovato da aggiungere.");
            }

            // Stampo il numero di dichiaranti da aggiornare   
            if (datiComplessivi.DichiarantiDaAggiornare.Count > 0)
            {
                logFile.LogInfo($"Trovati {datiComplessivi.DichiarantiDaAggiornare.Count} dichiaranti da aggiornare.");
            }
            else
            {
                logFile.LogInfo("Nessun dichiarante trovato da aggiornare.");
            }

        }
        catch (FileNotFoundException)
        {
            logFile.LogError($"Errore: Il file CSV non è stato trovato al percorso specificato: {percorsoFile}");
        }
        catch (Exception ex)
        {
            logFile.LogError($"Errore generico durante la lettura del file CSV: {ex.Message}");
        }

        return datiComplessivi;
    }

    // Funzione 2: Carica il file CSV delle utenze idriche e restituisce una lista di utenze da aggiungere o aggiornare
    public static DatiCsvCompilati LeggiFileUtenzeIdriche(string percorsoFile, int selectedEnteId, ApplicationDbContext _context, int idUser)
    {
        // Parte 1: Creazione della variabile da restituire e apertura file di log

        var datiComplessivi = new DatiCsvCompilati();
        FileLog logFile = new FileLog($"wwwroot/log/Lettura_UtenzeIdriche.log");  // Creo/Apro il file di log
        List<string> errori = new List<string>();
        List<string> warning = new List<string>();
        // Inizializzo una variabile per tenere traccia del numero di Indirizzi mal formati trovatti
        int countIndirizziMalFormati = 0;
        // int countVariazioneToponomi = 0;

        try
        {
            // Leggo il numero righe da processare escludendo l'intestazione

            var righe = File.ReadAllLines(percorsoFile).Skip(1);

            // Imposto la riga corrente ad 1 
            int rigaCorrente = 1;

            // Stampo dei messagi informativi sul file di log. In modo da tenere traccia di tutte le operazioni che vengono effetuate
            logFile.LogInfo($"Nuovo caricamento dati phirana id Ente: {selectedEnteId}");
            logFile.LogInfo($"Numero di righe da elaborare: {righe.Count()} ");

            // Carico le toponimie esistenti dal database per l'ente specificato
            var toponimi = _context.Toponomi.Where(s => s.IdEnte == selectedEnteId).ToList();

            // Inizializzo in modo sicuro la lista dei toponimi
            if (toponimi == null || toponimi.Count == 0)
            {
                // logFile.LogInfo($"AVVISO: La lista di toponimi per l'ente {selectedEnteId} è null. Ne creo una nuova.");
                toponimi = new List<Toponimo>();
            }


            // Carico tutte le utenze idrciche asssociate al ente

            var utenzeIdriche = _context.UtenzeIdriche.Where(s => s.IdEnte == selectedEnteId).ToList();

            // Verifico che utenzeIdriche non sia null

            if (utenzeIdriche == null)
            {
                // logFile.LogInfo($"AVVISO: La lista delle utenze Idriche per l'ente {selectedEnteId} è null. Ne creo una nuova.");
                utenzeIdriche = new List<UtenzaIdrica>();
            }

            // Inizio - Lettura delle varie righe

            foreach (var riga in righe)
            {
                var error = false;
                rigaCorrente++;

                // Parte 2: Verifiche Preliminari sui campi

                // a) verifico se la riga è vuota
                if (string.IsNullOrWhiteSpace(riga)) continue;

                var campi = riga.Split(CsvDelimiter);

                // b) Verifico che il file ha i campi minimi per poter effetuare le operazioni successive

                if (campi.Length < 39)
                {
                    errori.Add($"Attenzione: Riga {rigaCorrente} malformata, saltata. Numero di campi: {campi.Length}. Attesi almeno 39.");
                    continue;
                }

                // c) Verifico che esiste il campo idAcquedotto è presente

                if (string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[0])))
                {
                    errori.Add($"Attenzione: Id Acquedotto mancante, saltata. | Riga {rigaCorrente} | Nominativo: {FunzioniTrasversali.rimuoviVirgolette(campi[32])} {FunzioniTrasversali.rimuoviVirgolette(campi[33])} | Codice Fiscale: {FunzioniTrasversali.rimuoviVirgolette(campi[36])}");
                    error = true;
                }

                // d) Controllo se il campo Codice Fiscale è valido è != null

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[36])))
                {
                    // logFile.LogInfo($"SONO NULL! | SESSO: {FunzioniTrasversali.rimuoviVirgolette(campi[34])}");
                    if (!FunzioniTrasversali.rimuoviVirgolette(campi[34]).Equals("D", StringComparison.OrdinalIgnoreCase))
                    {
                        logFile.LogInfo("COD FISC MANCATE E sesso != D");
                        errori.Add($"Attenzione : Codice Fiscale mancante, saltata. | Riga {rigaCorrente} | Nominativo: {FunzioniTrasversali.rimuoviVirgolette(campi[32])} {FunzioniTrasversali.rimuoviVirgolette(campi[33])}");
                        error = true;
                    }
                    else if (string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[37])) &&
                            FunzioniTrasversali.rimuoviVirgolette(campi[9]) != "4" &&
                            FunzioniTrasversali.rimuoviVirgolette(campi[9]) != "5" &&
                            string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[14])) &&
                            FunzioniTrasversali.rimuoviVirgolette(campi[34]).Equals("D", StringComparison.OrdinalIgnoreCase))
                    {
                        warning.Add($"Attenzione: Codice Fiscale e Partita IVA della ditta {FunzioniTrasversali.rimuoviVirgolette(campi[32])} non presente. (Questo non è un errore, ma una segnalazione). | Riga {rigaCorrente}");
                        error = true;
                    }
                }
                else if (FunzioniTrasversali.rimuoviVirgolette(campi[36]).Length != 16)
                {
                    // logFile.LogInfo($"mal formata! | SESSO: {FunzioniTrasversali.rimuoviVirgolette(campi[34])}");
                    if (!FunzioniTrasversali.rimuoviVirgolette(campi[34]).Equals("D", StringComparison.OrdinalIgnoreCase))
                    {
                        // logFile.LogInfo("Sesso diverso da D!");
                        errori.Add($"Attenzione : Codice Fiscale mal formato, saltata. | Riga {rigaCorrente} | Nominativo: {FunzioniTrasversali.rimuoviVirgolette(campi[32])} {FunzioniTrasversali.rimuoviVirgolette(campi[33])}");
                        error = true;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[37])) &&
                            FunzioniTrasversali.rimuoviVirgolette(campi[9]) != "4" &&
                            FunzioniTrasversali.rimuoviVirgolette(campi[9]) != "5" &&
                            string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[14])) &&
                            FunzioniTrasversali.rimuoviVirgolette(campi[34]).Equals("D", StringComparison.OrdinalIgnoreCase))
                        {
                            // logFile.LogInfo("PIVA 2 NULL && stato != 4, e sesso =D");
                            warning.Add($"Attenzione: Codice Fiscale e Partita IVA della ditta {FunzioniTrasversali.rimuoviVirgolette(campi[32])} non trovati. (Questo non è un errore, ma una segnalazione). | Riga {rigaCorrente}");
                            error = true;
                        }
                    }
                }


                // e) Controllo se la matricola del contatore è presente

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[12])) && FunzioniTrasversali.rimuoviVirgolette(campi[9]) != "4" && FunzioniTrasversali.rimuoviVirgolette(campi[9]) != "5" && string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[14])))
                {

                    errori.Add($"Attenzione: Matricola Contatore mancante, saltata. Riga {rigaCorrente} | Nominativo: {FunzioniTrasversali.rimuoviVirgolette(campi[32])} {FunzioniTrasversali.rimuoviVirgolette(campi[33])} | Codice Fiscale: {FunzioniTrasversali.rimuoviVirgolette(campi[36])}");
                    error = true;
                }

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[0])))
                {
                    errori.Add($"Attenzione: Id Acquedotto mancante, saltata. Riga {rigaCorrente}  | Nominativo: {FunzioniTrasversali.rimuoviVirgolette(campi[32])} {FunzioniTrasversali.rimuoviVirgolette(campi[33])} | Codice Fiscale: {FunzioniTrasversali.rimuoviVirgolette(campi[36])}");
                    error = true;
                }

                // f) Controllo se i campi nomi, cognome e sesso sono presenti

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[32])) && string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[33])) && FunzioniTrasversali.rimuoviVirgolette(campi[34]).ToUpper() != "D")
                {
                    errori.Add($"Attenzione: Nome o Cognome mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[0])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[34])) ||
                    (FunzioniTrasversali.rimuoviVirgolette(campi[34]).ToUpper() != "M" && FunzioniTrasversali.rimuoviVirgolette(campi[34]).ToUpper() != "F" && FunzioniTrasversali.rimuoviVirgolette(campi[34]).ToUpper() != "D"))
                {
                    errori.Add($"Attenzione: Sesso mancante o mal formato, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[0])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                // g) Controllo se il campo periodoIniziale è presente

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[13])))
                {
                    errori.Add($"Attenzione: Periodo iniziale mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[0])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                // h) Verifico se il campo tipo utenza è presente

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[26])))
                {
                    errori.Add($"Attenzione: Tipo Utenza mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[0])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                // i) Verifico se il campo indirizzo Ubicazione è presente

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[15])))
                {
                    errori.Add($"Attenzione: Indirizzo ubicazione mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[0])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.FormattaNumeroCivico(campi[35])))
                {
                    errori.Add($"Attenzione: Data Nascita mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[0])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                // j) Verifico se il campo numero civico è presente

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.FormattaNumeroCivico(campi[16])))
                {
                    errori.Add($"Attenzione: Numero civico mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[0])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                // Parte 3: Verifica se l'indirizzo di ubicazione è associabile ad un toponimo

                //  a) Variabili di supporto
                var cod_fisc = FunzioniTrasversali.rimuoviVirgolette(campi[36]).ToUpper();
                var indirizzoUbicazione = FunzioniTrasversali.rimuoviVirgolette(campi[15]).ToUpper();
                var indirizzoFormattato = FunzioniTrasversali.FormattaIndirizzo(_context, indirizzoUbicazione, cod_fisc, selectedEnteId);
                string? indirizzoRicavato = indirizzoFormattato != null ? indirizzoFormattato.ToUpper() : null;

                int? idToponimo = null;

                // b) Controllo nel DB se esiste già il toponimo
                var toponimoTrova = _context.Toponomi
                    .FirstOrDefault(s => s.denominazione == indirizzoUbicazione && s.IdEnte == selectedEnteId);

                if (toponimoTrova != null)
                {
                    idToponimo = toponimoTrova.id;

                    // c) Aggiorno solo se non è già normalizzato e se ho un indirizzo ricavato valido
                    if (toponimoTrova.normalizzazione == null && indirizzoRicavato != null)
                    {
                        toponimoTrova.normalizzazione = indirizzoRicavato;
                        toponimoTrova.data_aggiornamento = DateTime.Now;
                        datiComplessivi.ToponimiDaAggiornare.Add(toponimoTrova);
                    }
                }
                else
                {
                    var toponimoLista = datiComplessivi.Toponimi?.FirstOrDefault(t => t.denominazione == indirizzoUbicazione && t.IdEnte == selectedEnteId);

                    if (toponimoLista != null)
                    {
                        // Caso: esiste già nella lista → aggiorno solo i campi mancanti
                        if (toponimoLista.normalizzazione == null && indirizzoRicavato != null)
                        {
                            toponimoLista.normalizzazione = indirizzoRicavato;
                            toponimoLista.data_aggiornamento = DateTime.Now;
                        }
                    }
                    else
                    {
                        // Caso: completamente nuovo → lo aggiungo alla lista
                        var nuovoToponimo = new Toponimo
                        {
                            denominazione = indirizzoUbicazione,
                            IdEnte = selectedEnteId,
                            data_creazione = DateTime.Now,
                            normalizzazione = indirizzoRicavato // valorizzo subito se disponibile
                        };

                        datiComplessivi.Toponimi?.Add(nuovoToponimo);
                    }
                }

                // Parte 4: Mi ricavo l'id del dichiarante associato all'utenza

                var cognome = FunzioniTrasversali.rimuoviVirgolette(campi[32]).ToUpper();
                var nome = FunzioniTrasversali.rimuoviVirgolette(campi[33]).ToUpper();

                var dichiaranteTrovato = _context.Dichiaranti.FirstOrDefault(d => d.Cognome == cognome && d.Nome == nome && d.CodiceFiscale == cod_fisc && d.IdEnte == selectedEnteId);
                int? idDichiarante = dichiaranteTrovato != null ? dichiaranteTrovato.id : (int?)null;

                // Parte 5: Controllo se sono presenti Errori

                if (error)
                {
                    continue; // Salta la riga se ci sono errori
                }

                // Creo un utenza

                var utenza = new UtenzaIdrica
                {
                    idAcquedotto = FunzioniTrasversali.rimuoviVirgolette(campi[0]),
                    stato = int.TryParse(FunzioniTrasversali.rimuoviVirgolette(campi[9]), out int stato) ? stato : 0,
                    periodoIniziale = FunzioniTrasversali.ConvertiData(FunzioniTrasversali.rimuoviVirgolette(campi[13])),
                    periodoFinale = FunzioniTrasversali.ConvertiData(FunzioniTrasversali.rimuoviVirgolette(campi[14])),
                    matricolaContatore = FunzioniTrasversali.rimuoviVirgolette(campi[12]).ToUpper(),
                    indirizzoUbicazione = indirizzoUbicazione,
                    numeroCivico = FunzioniTrasversali.FormattaNumeroCivico(campi[16]).ToUpper(),
                    subUbicazione = FunzioniTrasversali.rimuoviVirgolette(campi[17]).ToUpper(),
                    scalaUbicazione = FunzioniTrasversali.rimuoviVirgolette(campi[18]),
                    piano = FunzioniTrasversali.rimuoviVirgolette(campi[19]),
                    interno = FunzioniTrasversali.rimuoviVirgolette(campi[20]),
                    tipoUtenza = FunzioniTrasversali.rimuoviVirgolette(campi[26]).ToUpper(),
                    cognome = cognome,
                    nome = nome,
                    sesso = FunzioniTrasversali.rimuoviVirgolette(campi[34]).ToUpper(),
                    DataNascita = FunzioniTrasversali.ConvertiData(FunzioniTrasversali.rimuoviVirgolette(campi[35])),
                    codiceFiscale = cod_fisc,
                    partitaIva = FunzioniTrasversali.rimuoviVirgolette(campi[37]),
                    data_creazione = DateTime.Now,
                    IdEnte = selectedEnteId,
                    idToponimo = idToponimo,
                    IdUser = idUser,
                    IdDichiarante = idDichiarante,
                };

                if (idToponimo != null)
                {
                    utenza.idToponimo = idToponimo;
                }
                else
                {
                    utenza.idToponimo = null;
                }

                // Parte 6: Verifico se l'utenza è gia presente

                var utenzaEsistente = utenzeIdriche.FirstOrDefault(u =>
                    u.idAcquedotto == utenza.idAcquedotto &&
                    u.codiceFiscale == utenza.codiceFiscale);

                // caso a) Se l'utenza non esiste allora la creo
                if (utenzaEsistente == null)
                {
                    datiComplessivi.UtenzeIdriche.Add(utenza);
                }
                else
                {
                    // Caso b) se l'utenza esiste allora vado a verificare se ci sono campi da aggiornare
                    bool aggiornare = false;
                    // Inzio - Confronto tra i dati del db e quelli del csv
                    // logFile.LogInfo($"Pre-Vertifica: {utenza.ToString()}");

                    if (utenzaEsistente.stato != null && utenza.stato != null && utenzaEsistente.stato != utenza.stato)
                    {
                        // logFile.LogInfo($"Aggiorno lo stato da {utenzaEsistente.stato} a {utenza.stato}");
                        utenzaEsistente.stato = utenza.stato;
                        aggiornare = true;
                    }

                    if (utenzaEsistente.periodoIniziale != null && utenza.periodoIniziale != null && utenzaEsistente.periodoIniziale != utenza.periodoIniziale)
                    {
                        // logFile.LogInfo($"Aggiorno Periodo Iniziale da {utenzaEsistente.periodoIniziale} a {utenza.periodoIniziale}");
                        utenzaEsistente.periodoIniziale = utenza.periodoIniziale;
                        aggiornare = true;
                    }

                    if (utenza.periodoFinale != null && utenzaEsistente.periodoFinale != null && utenzaEsistente.periodoFinale != utenza.periodoFinale)
                    {
                        // logFile.LogInfo($"Aggiorno Periodo Finale da {utenzaEsistente.periodoFinale} a {utenza.periodoFinale}");
                        utenzaEsistente.periodoFinale = utenza.periodoFinale;
                        aggiornare = true;
                    }

                    if (string.IsNullOrEmpty(utenzaEsistente.matricolaContatore) && string.IsNullOrEmpty(utenza.matricolaContatore) && !string.Equals(utenzaEsistente.matricolaContatore, utenza.matricolaContatore, StringComparison.OrdinalIgnoreCase))
                    {
                        // logFile.LogInfo($"Aggiorno Matricola Contatre da {utenzaEsistente.matricolaContatore} a {utenza.matricolaContatore}");
                        utenzaEsistente.matricolaContatore = utenza.matricolaContatore;
                        aggiornare = true;
                    }

                    if (!string.Equals(utenzaEsistente.indirizzoUbicazione, utenza.indirizzoUbicazione, StringComparison.OrdinalIgnoreCase))
                    {
                        // logFile.LogInfo($"Aggiorno Indirizzo Ubicazione da {utenzaEsistente.indirizzoUbicazione} a {utenza.indirizzoUbicazione}");
                        utenzaEsistente.indirizzoUbicazione = utenza.indirizzoUbicazione;
                        aggiornare = true;
                    }

                    if (!string.Equals(utenzaEsistente.numeroCivico, utenza.numeroCivico, StringComparison.OrdinalIgnoreCase))
                    {
                        // logFile.LogInfo($"Aggiorno Numero Civico da {utenzaEsistente.numeroCivico} a {utenza.numeroCivico}");
                        utenzaEsistente.numeroCivico = utenza.numeroCivico;
                        aggiornare = true;
                    }

                    // if (!string.Equals(utenzaEsistente.subUbicazione, utenza.subUbicazione, StringComparison.OrdinalIgnoreCase))
                    // {
                    //     utenzaEsistente.subUbicazione = utenza.subUbicazione;
                    //     aggiornare = true;
                    // }

                    // if (!string.Equals(utenzaEsistente.scalaUbicazione, utenza.scalaUbicazione, StringComparison.OrdinalIgnoreCase))
                    // {
                    //     utenzaEsistente.scalaUbicazione = utenza.scalaUbicazione;
                    //     aggiornare = true;
                    // }

                    // if (!string.Equals(utenzaEsistente.piano, utenza.piano, StringComparison.OrdinalIgnoreCase))
                    // {
                    //     utenzaEsistente.piano = utenza.piano;
                    //     aggiornare = true;
                    // }

                    // if (!string.Equals(utenzaEsistente.interno, utenza.interno, StringComparison.OrdinalIgnoreCase))
                    // {
                    //     utenzaEsistente.interno = utenza.interno;
                    //     aggiornare = true;
                    // }

                    if (string.IsNullOrEmpty(utenzaEsistente.tipoUtenza) && string.IsNullOrEmpty(utenza.tipoUtenza) && !string.Equals(utenzaEsistente.tipoUtenza, utenza.tipoUtenza, StringComparison.OrdinalIgnoreCase))
                    {
                        // logFile.LogInfo($"Aggiorno Tipo Utenza da {utenzaEsistente.tipoUtenza} a {utenza.tipoUtenza}");
                        utenzaEsistente.tipoUtenza = utenza.tipoUtenza;
                        aggiornare = true;
                    }

                    if (utenzaEsistente.cognome != null && utenza.cognome != null && !string.Equals(utenzaEsistente.cognome, utenza.cognome, StringComparison.OrdinalIgnoreCase))
                    {
                        // logFile.LogInfo($"Aggiorno cognome da {utenzaEsistente.cognome} a {utenza.cognome}");
                        utenzaEsistente.cognome = utenza.cognome;
                        aggiornare = true;
                    }

                    if (utenzaEsistente.nome != null && utenza.nome != null && !string.Equals(utenzaEsistente.nome, utenza.nome, StringComparison.OrdinalIgnoreCase))
                    {
                        // logFile.LogInfo($"Aggiorno nome da {utenzaEsistente.nome} a {utenza.nome}");
                        utenzaEsistente.nome = utenza.nome;
                        aggiornare = true;
                    }

                    if (utenzaEsistente.sesso != null && utenza.sesso != null && !string.Equals(utenzaEsistente.sesso, utenza.sesso, StringComparison.OrdinalIgnoreCase))
                    {
                        // logFile.LogInfo($"Aggiorno sesso da {utenzaEsistente.sesso} a {utenza.sesso}");
                        utenzaEsistente.sesso = utenza.sesso;
                        aggiornare = true;
                    }

                    if (utenzaEsistente.codiceFiscale != null && utenza.codiceFiscale != null && !string.Equals(utenzaEsistente.codiceFiscale, utenza.codiceFiscale, StringComparison.OrdinalIgnoreCase))
                    {
                        // logFile.LogInfo($"Aggiorno Codice Fiscale da {utenzaEsistente.codiceFiscale} a {utenza.codiceFiscale}");
                        utenzaEsistente.codiceFiscale = utenza.codiceFiscale;
                        aggiornare = true;
                    }

                    if (utenzaEsistente.partitaIva != null && utenza.partitaIva != null && !string.Equals(utenzaEsistente.partitaIva, utenza.partitaIva, StringComparison.OrdinalIgnoreCase))
                    {
                        // logFile.LogInfo($"Aggiorno Partita IVA da {utenzaEsistente.partitaIva} a {utenza.partitaIva}");
                        utenzaEsistente.partitaIva = utenza.partitaIva;
                        aggiornare = true;
                    }

                    if (utenzaEsistente.idToponimo != null && utenza.idToponimo != null && utenzaEsistente.idToponimo != utenza.idToponimo)
                    {
                        // countVariazioneToponomi++;
                        // logFile.LogInfo($"Aggiorno idToponimo da {utenzaEsistente.idToponimo} a {utenza.idToponimo}");
                        utenzaEsistente.idToponimo = utenza.idToponimo;
                        aggiornare = true;
                    }

                    if (utenzaEsistente.IdDichiarante != null && utenza.IdDichiarante != null && utenzaEsistente.IdDichiarante != utenza.IdDichiarante)
                    {
                        // logFile.LogInfo($"Aggiorno IdDichiarante da {utenzaEsistente.IdDichiarante} a {utenza.IdDichiarante}");
                        utenzaEsistente.IdDichiarante = utenza.IdDichiarante;
                        aggiornare = true;
                    }

                    // Fine - Confronto tra i dati del db e quelli del csv

                    // Infine verifico se devo aggiornare dei dati
                    if (aggiornare)
                    {
                        // logFile.LogInfo($"Aggiorno: {utenzaEsistente.ToString()}");
                        datiComplessivi.UtenzeIdricheEsistente.Add(utenzaEsistente);
                    }

                }
            }

            // Fine - lettura delle righe

            // Parte 7 : Scrittura dei log sul file corrispetivo

            // a) Scrivo prima i log di errore se sono presenti
            if (errori.Count > 0)
            {
                logFile.LogInfo($"Errori riscontrati durante l'elaborazione: {errori.Count} ");
                foreach (var errore in errori)
                {
                    logFile.LogError(errore);
                }
            }
            else
            {
                logFile.LogInfo("Elaborazione completata senza errori.");
            }

            // b) succesivamente scrivo nel file di log i warning riscontrati se presenti
            if (warning.Count > 0)
            {
                logFile.LogInfo($"Warning riscontrati durante l'elaborazione: {warning.Count} ");
                foreach (var w in warning) // Cambiato nome variabile per evitare conflitto
                {
                    logFile.LogWarning(w);
                }
            }
            else
            {
                logFile.LogInfo("Elaborazione completata senza warning.");
            }

            // c) Scrivo il numero di toponimi aggiunti nel DB se presenti
            if (datiComplessivi.Toponimi?.Count > 0)
            {
                logFile.LogInfo($"Numero di Toponimi Aggiunti: {datiComplessivi.Toponimi.Count}");
            }
            else
            {
                logFile.LogInfo("Nessun toponimo aggiunto");
            }

            // d) Scrivo il numero di toponomi aggiornati nel BD se presenti
            if (datiComplessivi.ToponimiDaAggiornare.Count > 0)
            {
                logFile.LogInfo($"Toponimi Aggiornati: {datiComplessivi.ToponimiDaAggiornare.Count}");
            }
            else
            {
                logFile.LogInfo($"Nessun toponimo aggiornato!");
            }

            // e) Stampo il numero totale di indirizzi mal formati trovati
            // countIndirizziMalFormati = countIndirizziMalFormati - countVariazioneToponomi;
            if (countIndirizziMalFormati > 0)
            {
                datiComplessivi.countIndirizziMalFormati = countIndirizziMalFormati;
                logFile.LogInfo($"indirizzi malformati Trovato: {countIndirizziMalFormati}");
            }
            else
            {
                datiComplessivi.countIndirizziMalFormati = 0;
                logFile.LogInfo($"Non sono stati riscontrati indirizzi mal formati!");
            }

        }
        catch (FileNotFoundException)
        {
            logFile.LogError($"Errore: Il file CSV non è stato trovato al percorso specificato: {percorsoFile}");
        }
        catch (Exception ex)
        {
            logFile.LogError($"Errore generico durante la lettura del file CSV: {ex.Message}");
        }

        return datiComplessivi;
    }

    // Funzione 3: Legge il file CSV proveniente da INPS per generare i report delle persone con bonus idrico
    public static DatiCsvCompilati LeggiFileINPS(string percorsoFile, ApplicationDbContext context, int selectedEnteId, int idUser, int? serie, bool confrontoCivico, bool escludiComponenti)
    {
        // Parte 1: Inizializzazione delle variabili
        var datiComplessivi = new DatiCsvCompilati();
        FileLog logFile = new FileLog($"wwwroot/log/Elaborazione_INPS.log");
        List<string> errori = new List<string>();
        DateTime dataElaborazione = DateTime.Now; // Neccessaria per impostarla nei report ed evitare divisioni
        try
        {
            var righe = File.ReadAllLines(percorsoFile).Skip(1);

            int rigaCorrente = 1;
            logFile.LogInfo($"Numero di righe da elaborare: {righe.Count()}");
            // logFile.LogInfo($"Confranto Numero civico: {confrontoCivico.ToString()}");
            // var dichiaranti = context.Dichiaranti.ToList();

            // Parte 2: Lettura delle varie righe
            foreach (var riga in righe)
            {
                //logFile.LogInfo($"Sto elaborando la riga {rigaCorrente}");
                var error = false;
                rigaCorrente++;
                // Parte 3: Verifiche Preliminari sui campi

                // verifico se la riga è vuota
                if (string.IsNullOrWhiteSpace(riga)) continue;

                var campi = riga.Split(CsvDelimiter);

                // Verifico se la riga ha almeno 1 campi
                if (campi.Length < 16)
                {
                    errori.Add($"Riga {rigaCorrente} malformata, saltata. Numero di campi: {campi.Length}. Attesi almeno 16.");
                    error = true;
                }

                // Verifico se il campo idAto è presente e nonn vuoto
                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[0])))
                {
                    errori.Add($"ID_ATO mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo codice_bonus è presente, non vuoto è ha una lunhezza di 15 carratteri

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[1])) || FunzioniTrasversali.rimuoviVirgolette(campi[1]).Length != 15)
                {
                    errori.Add($"Attenzione: Codice Bonus mancante o non valido, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo Codice Fiscale è presente e non vuoto

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[2])))
                {
                    errori.Add($"Attenzione: Codice Fiscale mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo NOME_DICHIARANTE e COGNOME_DICHIARANTE sono presenti e non vuoti
                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[3])) || string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[4])))
                {
                    errori.Add($"Attenzione: Nome o Cognome mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo Anno_validità è presente e non vuoto

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[6])))
                {
                    errori.Add($"Attenzione: Anno di validità mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // verifico se il campo Data_inizio_validità è presente e non vuoto
                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[7])))
                {
                    errori.Add($"Attenzione: Data di inizio validità mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo Data_fine_validità è presente e non vuoto
                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[8])))
                {
                    errori.Add($"Attenzione: Data di fine validità mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo indirizzo_abitazione è presente e non vuoto
                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[9])))
                {
                    errori.Add($"Attenzione: Indirizzo abitazione mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo numero_civico è presente e non vuoto
                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[10])))
                {
                    errori.Add($"Attenzione: Numero civico mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo iSTAT è presente e non vuoto
                string istat = FunzioniTrasversali.rimuoviVirgolette(campi[11]);

                if (string.IsNullOrWhiteSpace(istat) || istat.Length != 6)
                {
                    errori.Add($"Attenzione: ISTAT mancante o malformata, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // VERIFICO Se il campo cap è presente e non vuoto
                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[12])) || FunzioniTrasversali.rimuoviVirgolette(campi[12]).Length != 5)
                {
                    errori.Add($"Attenzione: CAP mancante o malformato, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // vrifico se il campo provincia_abitazione è presente e non vuoto
                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[13])) || FunzioniTrasversali.rimuoviVirgolette(campi[13]).Length != 2)
                {
                    errori.Add($"Attenzione: Provincia abitazione mancante o malformata, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo Presenza_POD è presente ed è valido
                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[14])) ||
                    (!FunzioniTrasversali.rimuoviVirgolette(campi[14]).Equals("SI", StringComparison.OrdinalIgnoreCase) &&
                     !FunzioniTrasversali.rimuoviVirgolette(campi[14]).Equals("NO", StringComparison.OrdinalIgnoreCase)))
                {
                    errori.Add($"Attenzione: Presenza POD mancante o non valida, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // verifdico se il campo n_componenti è presente e non vuoto
                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[15])))
                {
                    errori.Add($"Attenzione: Numero componenti mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Parte 4: Controllo se sono presenti Errori

                if (error)
                {
                    continue; // Salta la riga se ci sono errori
                }

                //logFile.LogError($"Riga {rigaCorrente} Error: {error}");

                // Parte 5: Elaborazione della riga

                // 1.b) Mi salvo i campi presi dal file CSV in modo da poter effettuare le operazioni successive
                // logFile.LogInfo($"Sto Inizializzando le variabili con i dati del File CSV!");
                string? idAto = FunzioniTrasversali.rimuoviVirgolette(campi[0]);
                string? codiceBonus = FunzioniTrasversali.rimuoviVirgolette(campi[1]).ToUpper();
                string? codiceFiscale = FunzioniTrasversali.rimuoviVirgolette(campi[2]).ToUpper();

                string? nomeDichiarante = FunzioniTrasversali.rimuoviVirgolette(campi[3]).ToUpper();
                string? cognomeDichiarante = FunzioniTrasversali.rimuoviVirgolette(campi[4]).ToUpper();
                string[]? codiciFiscaliFamigliari = FunzioniTrasversali.splitCodiceFiscale(campi[5]);

                string? annoValidita = FunzioniTrasversali.rimuoviVirgolette(campi[6]);
                DateTime dataInizioValidita = FunzioniTrasversali.ConvertiData(campi[7], DateTime.MinValue);
                DateTime dataFineValidita = FunzioniTrasversali.ConvertiData(campi[8], DateTime.MinValue);

                string? indirizzoAbitazione = FunzioniTrasversali.rimuoviVirgolette(campi[9]).ToUpper();
                string? numeroCivico = FunzioniTrasversali.FormattaNumeroCivico(campi[10]).ToUpper();
                string? istatAbitazione = FunzioniTrasversali.rimuoviVirgolette(campi[11]).ToUpper();
                string? capAbitazione = FunzioniTrasversali.rimuoviVirgolette(campi[12]).ToUpper();
                string? provinciaAbitazione = FunzioniTrasversali.rimuoviVirgolette(campi[13]).ToUpper();

                string presenzaPod = FunzioniTrasversali.rimuoviVirgolette(campi[14]).ToUpper();
                string numeroComponenti_str = FunzioniTrasversali.rimuoviVirgolette(campi[15]).ToUpper();
                int numeroComponenti = int.Parse(numeroComponenti_str);
                // logFile.LogInfo("Ho istanziato tutti i campi con i dati del CSV");

                //DateTime dataCreazione = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0); // Escludo i secondi della data attuale per evitare problemi successivi
                // logFile.LogInfo($"Ho inizializatto la data di creazione: {dataCreazione}");

                // 1.c) Aggiungo dei campi aggiuntivi neccessari per la creazione del report
                string esitoStr = "No";
                string esito = "04"; // 01 = fornitura diretta, 02 = fornitura indiretta, 03 = fornitura diretta non rispetta requisiti, 04 =  fornitura indiretta non rispetta requisiti 
                int? idFornituraIdrica = null;
                string? note = null;

                // 2.a) mi salvo i campi relativi all'ente in modo da poter effetuare le operazioni successive

                var enti = context.Enti.Where(d => d.id == selectedEnteId).ToList();

                if (enti.Count == 0)
                {
                    errori.Add($"Attenzione: Nessun ente trovato con l'ID selezionato {selectedEnteId}. Riga {rigaCorrente}");
                    continue; // Salta la riga se non c'è l'ente
                }

                var ente = enti.First();
                // var nomeEnte = ente.nome;
                var codiceFiscaleEnte = ente.CodiceFiscale;
                var istatEnte = ente.istat;
                var capEnte = ente.Cap;

                string? messaggio = null;
                int? mc = null;
                bool verificare = false;
                int? idDichiarante = null;
                string? codiceFiscaleDichiarateTrovato = null;
                int? idUtenza = null;
                // 2.b) Verifico se i campi ISTAT, CAP e provincia corrispondono a l'ente selezionato il quale gestisce le utenze idriche
                // logFile.LogInfo($"Riga: {rigaCorrente} sto verificando l'indirizzo!");
                if (!(istatEnte != istatAbitazione || capEnte != capAbitazione || provinciaAbitazione != ente.Provincia))
                {
                    // 2.c) se i campi corispondono verifico se il richiedente è residente nel comune selezionato
                    var dichiaranteTrovato = context.Dichiaranti.FirstOrDefault(s => s.CodiceFiscale == codiceFiscale && s.IdEnte == selectedEnteId);
                    if (dichiaranteTrovato != null)
                    {
                        // logFile.LogInfo($"Ho trovato un dichiarante: {dichiaranteTrovato.ToString()}");
                        // 2.c.1) se è residente nel comune selzionato allora esito è uguale a Si
                        esitoStr = "Si";
                        idDichiarante = dichiaranteTrovato.id;
                        //3.a) verifica se il richiedente ha una fornitura idrica diretta 

                        // logFile.LogInfo("Pre verifica esistenza fornitura");
                        (string esitoRestituito, int? idFornituraTrovato, string? messagge, int? idUtenzaDichiarante) = FunzioniTrasversali.VerificaEsistenzaFornitura(dichiaranteTrovato, selectedEnteId, context, indirizzoAbitazione, numeroCivico, confrontoCivico);
                        idFornituraIdrica = idFornituraTrovato;
                        // logFile.LogInfo($"Post pre verifica fornitura. Dati restituiti. Esito: {esitoRestituito} | id Fornitura: {idFornituraTrovato} | id Utenza {idUtenza} | Messaggio:\n {messagge}");

                        // Resetto il messaggio

                        if (messagge != "Nessuna fornitura trovata per il dichiarante.")
                        {
                            note = note + messagge;
                        }

                        // Verifico l'esito ottenuto e lo salco nella variabile solo se è diverso da 04
                        if (esitoRestituito == "01")
                        {
                            esito = "01";
                            idUtenza = idUtenzaDichiarante;
                            codiceFiscaleDichiarateTrovato = codiceFiscale;
                        }
                        else if (esitoRestituito == "03")
                        {
                            esito = "03";
                            idUtenza = idUtenzaDichiarante;
                            codiceFiscaleDichiarateTrovato = codiceFiscale;
                        }
                        else if (esitoRestituito == "04")
                        {
                            if (dichiaranteTrovato.NumeroComponenti > 1)
                            {
                                // logFile.LogInfo("Cerco i Famigliari!");
                                // Cerco tra i famigliari
                                var today = DateTime.Today;
                                var cutoff = today.AddYears(-18);

                                var famigliari = context.Dichiaranti
                                    .Where(s =>
                                        (s.CodiceFamiglia == dichiaranteTrovato.CodiceFamiglia
                                        || s.CodiceFiscaleIntestatarioScheda == dichiaranteTrovato.CodiceFiscaleIntestatarioScheda)
                                        && s.IdEnte == selectedEnteId
                                        && s.CodiceFiscale != dichiaranteTrovato.CodiceFiscale
                                        && s.DataNascita <= cutoff
                                    )
                                    .ToList();

                                if (famigliari.Count > 0)
                                {
                                    foreach (var membro in famigliari)
                                    {
                                        // logFile.LogInfo($"Verifica 2 Esistenza Fornitura. Membro: {membro.ToString()}");

                                        (string esitoFamigliare, int? idFornituraMembro, string? messaggeFamigliare, int? idUtenzaMembro) = FunzioniTrasversali.VerificaEsistenzaFornitura(membro, selectedEnteId, context, indirizzoAbitazione, numeroCivico, confrontoCivico);
                                        idFornituraIdrica = idFornituraMembro;
                                        // logFile.LogInfo($"Post pre verifica fornitura 2. Dati restituiti. Esito: {esitoRestituito} | id Fornitura: {idFornituraTrovato} | id Utenza {idUtenza} | Messaggio:\n {messagge}");

                                        if (messaggeFamigliare != "Nessuna fornitura trovata per il dichiarante.")
                                        {
                                            note = note + messaggeFamigliare;
                                        }

                                        if (esitoFamigliare == "01")
                                        {
                                            idUtenza = idUtenzaMembro;
                                            codiceFiscaleDichiarateTrovato = membro.CodiceFiscale;
                                            note = null;
                                            esito = "01"; // Se uno dei membri della famiglia ha una fornitura diretta, l'esito è 01
                                            break;
                                        }
                                        else if (esitoFamigliare == "03")
                                        {
                                            idUtenza = idUtenzaMembro;
                                            codiceFiscaleDichiarateTrovato = membro.CodiceFiscale;
                                            esito = "03";
                                            break;
                                        }
                                        else if (esitoFamigliare == "04")
                                        {
                                            //codiceFiscaleDichiarateTrovato = codiceFiscale;
                                            // 3.g) Verifico se Presenza_POD è SI
                                            if (presenzaPod.Equals("Si", StringComparison.OrdinalIgnoreCase))
                                            {
                                                esito = "02"; // Se nessun membro della famiglia ha una fornitura diretta, ma Presenza_POD è SI, l'esito è 02
                                            }
                                        }

                                    }

                                }
                                else
                                {
                                    if (presenzaPod.Equals("Si", StringComparison.OrdinalIgnoreCase))
                                    {
                                        esito = "02"; // Se nessun membro della famiglia ha una fornitura diretta, ma Presenza_POD è SI, l'esito è 02
                                    }
                                }

                            }
                            else
                            {
                                if (presenzaPod.Equals("Si", StringComparison.OrdinalIgnoreCase))
                                {
                                    esito = "02"; // Se nessun membro della famiglia ha una fornitura diretta, ma Presenza_POD è SI, l'esito è 02
                                }
                            }
                        }

                        // Verifico se il numero di componenti fornito corisponte a quello effetivo di selene
                        if (dichiaranteTrovato.NumeroComponenti != numeroComponenti && !escludiComponenti)
                        {
                            if (ente.Selene == true)
                            {
                                note = note + $"\nAttenzione: Il numero di componenti fornito ({numeroComponenti}) non corrisponde a quello effettivo ({dichiaranteTrovato.NumeroComponenti}). é stato impostato come valore quello ricavato dal anagrafe.";
                                numeroComponenti = dichiaranteTrovato.NumeroComponenti;
                            }
                            else
                            {
                                note = note + $"\nAttenzione: Il numero di componenti fornito ({numeroComponenti}) non corrisponde a quello effettivo ({dichiaranteTrovato.NumeroComponenti}).";
                            }
                            logFile.LogWarning($"Attenzione: Il numero di componenti fornito ({numeroComponenti}) non corrisponde a quello effettivo ({dichiaranteTrovato.NumeroComponenti}). Codice Bonus: {codiceBonus} | Codice Fiscale: {codiceFiscale}");
                        }
                    }

                }
                else
                {
                    note = "Attenzione: I campi ISTAT o CAP non coincido con l'ente selezionato. ";
                    logFile.LogWarning($"Attenzone i campi ISTAT o CAP non coincido con l'ente selezionato. ISTAT: {istat} | Cap: {capAbitazione} | Codice Bonus: {codiceBonus} | Codice Fiscale: {codiceFiscale}");
                }

                // Aggiungo eventuali messaggi a note
                if (note != null && !string.IsNullOrWhiteSpace(note))
                {
                    verificare = true;
                    note = note + messaggio;
                }

                // Dati neccessari per l'esportazione siscom

                if (esito == "01" || esito == "02")
                {

                    // Calcolo differenza giorni (arrotondati all’intero)
                    int giorni = (int)(dataFineValidita.Date - dataInizioValidita.Date).TotalDays;

                    // Evito valori negativi
                    if (giorni < 0) giorni = 0;

                    mc = CSVReader.calcolaMC(giorni, numeroComponenti);
                }

                // Se il check è attivo allore imposto come valore di serie quello di default della scheda ente
                int valueSerie;
                if (serie == null)
                {
                    valueSerie = context.Enti.Where(s => s.id == selectedEnteId).Select(s => s.Serie).FirstOrDefault();
                }
                else
                {
                    valueSerie = (int)serie;
                }

                // logFile.LogInfo("Sto creando il report");

                // 4) Creo un nuovo report con i dati raccolti
                var report = new Report
                {
                    idAto = idAto,
                    codiceBonus = codiceBonus,
                    idFornitura = idFornituraIdrica,
                    codiceFiscaleRichiedente = codiceFiscale,
                    codiceFiscaleUtenzaTrovata = codiceFiscaleDichiarateTrovato,
                    idUtenza = idUtenza,
                    nomeDichiarante = nomeDichiarante,
                    cognomeDichiarante = cognomeDichiarante,
                    idDichiarante = idDichiarante,
                    annoValidita = annoValidita,
                    dataInizioValidita = dataInizioValidita,
                    dataFineValidita = dataFineValidita,
                    indirizzoAbitazione = indirizzoAbitazione,
                    numeroCivico = numeroCivico,
                    istat = istatAbitazione,
                    capAbitazione = capAbitazione,
                    provinciaAbitazione = provinciaAbitazione,
                    presenzaPod = presenzaPod,
                    numeroComponenti = numeroComponenti,
                    esitoStr = esitoStr,
                    esito = esito,
                    serie = valueSerie,
                    mc = mc,
                    incongruenze = verificare,
                    note = note != null ? note.Trim() : null,
                    IdEnte = selectedEnteId,
                    IdUser = idUser,
                    DataCreazione = dataElaborazione
                };

                // logFile.LogInfo($"Report Creato riga: {rigaCorrente}: {report.ToString()}");

                // Parte 6: Verifico se il report è gia presente

                var reportEsistente = context.Reports.FirstOrDefault(r => r.codiceBonus == report.codiceBonus && r.IdEnte == selectedEnteId);

                if (reportEsistente == null)
                {
                    // logFile.LogInfo("Il report non esiste");
                    datiComplessivi.reports.Add(report);
                }
                else
                {
                    // logFile.LogInfo("Sto Verificando se devo aggiornare i dati");
                    // Report esistente, verifico se ci sono campi da aggiornare
                    bool aggiornare = false;

                    // Inzio - Confronto tra i dati del db e quelli del csv
                    // Verifico campo per campo se ci sono differenze per il campo idAto
                    if (report.idAto != null && reportEsistente.idAto != null && reportEsistente.idAto != report.idAto)
                    {
                        reportEsistente.idAto = report.idAto;
                        aggiornare = true;
                    }

                    // Verifico campo per campo se ci sono differenze per il campo codiceFiscaleRichiedente

                    if (report.codiceFiscaleRichiedente != null && reportEsistente.codiceFiscaleRichiedente != null && reportEsistente.codiceFiscaleRichiedente != report.codiceFiscaleRichiedente)
                    {
                        reportEsistente.codiceFiscaleRichiedente = report.codiceFiscaleRichiedente;
                        aggiornare = true;
                    }

                    // Verifico il campo numeroComponenti
                    if (reportEsistente.numeroComponenti != report.numeroComponenti)
                    {
                        reportEsistente.numeroComponenti = report.numeroComponenti;
                        aggiornare = true;
                    }
                    

                    // Verifico il campo codiceFiscaleUtenzaTrovata
                    if (reportEsistente.codiceFiscaleUtenzaTrovata != report.codiceFiscaleUtenzaTrovata)
                    {
                        reportEsistente.codiceFiscaleUtenzaTrovata = report.codiceFiscaleUtenzaTrovata;
                        aggiornare = true;
                    }

                    // Verifico campo per campo se ci sono differenze per il campo esito

                    if (report.esito != null && reportEsistente.esito != null && reportEsistente.esito != report.esito)
                    {
                        reportEsistente.esito = report.esito;
                        aggiornare = true;
                    }

                    // Verifico campo per campo se ci sono differenze per il campo esitoStr

                    if (report.esitoStr != null && reportEsistente.esitoStr != null && reportEsistente.esitoStr != report.esitoStr)
                    {
                        reportEsistente.esitoStr = report.esitoStr;
                        aggiornare = true;
                    }

                    // Verifico campo per campo se ci sono differenze per il campo idFornitura

                    if (report.idFornitura != null && reportEsistente.idFornitura != null && reportEsistente.idFornitura != report.idFornitura)
                    {
                        reportEsistente.idFornitura = report.idFornitura;
                        aggiornare = true;
                    }

                    // Verifico campo per campo se ci sono differenze per il campo mc

                    if (reportEsistente.mc != report.mc)
                    {
                        reportEsistente.mc = report.mc;
                        aggiornare = true;
                    }

                    // Verifico campo per campo se ci sono differenze per il campo inizioValidita

                    if (reportEsistente.dataInizioValidita != report.dataInizioValidita)
                    {
                        reportEsistente.dataInizioValidita = report.dataInizioValidita;
                        aggiornare = true;
                    }

                    // Verifico campo per campo se ci sono differenze per il campo fineValidita

                    if (reportEsistente.dataFineValidita != report.dataFineValidita)
                    {
                        reportEsistente.dataFineValidita = report.dataFineValidita;
                        aggiornare = true;
                    }

                    // Verifico se il campo serie e diverso 

                    if (report.serie != reportEsistente.serie)
                    {
                        reportEsistente.serie = report.serie;
                        aggiornare = true;
                    }

                    // Verifico se il campo incongruenze è diverso
                    if (report.incongruenze != reportEsistente.incongruenze)
                    {
                        reportEsistente.incongruenze = report.incongruenze;
                        aggiornare = true;
                    }

                    // Verifico se il campo note è diverso
                    if (reportEsistente.note != report.note)
                    {
                        reportEsistente.note = report.note;
                        aggiornare = true;
                    }

                    // Verifico il campo idDichiarante

                    if (report.idDichiarante != reportEsistente.idDichiarante)
                    {
                        reportEsistente.idDichiarante = report.idDichiarante;
                        aggiornare = true;
                    }

                    // Verifico il campo idUtenza

                    if (report.idUtenza != reportEsistente.idUtenza)
                    {
                        reportEsistente.idUtenza = report.idUtenza;
                        aggiornare = true;
                    }


                    // Verifico se devo aggiornare dei dati

                    if (aggiornare)
                    {
                        // logFile.LogInfo("Aggiorno i dati e imposto la data d'aggiornamento");
                        reportEsistente.DataAggiornamento = DateTime.Now;
                        datiComplessivi.reportsDaAggiornare.Add(reportEsistente);
                    }

                }

            }
            // Fine - lettura delle righe
            // Parte 7 : Scrittura dei log sul file corrispetivo
            if (errori.Count > 0)
            {
                logFile.LogInfo($"Errori riscontrati {errori.Count} durante l'elaborazione:");
                foreach (var errore in errori)
                {
                    logFile.LogError(errore);
                }
            }
            else
            {
                logFile.LogInfo("Elaborazione completata senza errori.");
            }
        }
        catch (FileNotFoundException)
        {
            logFile.LogError($"Errore: Il file CSV non è stato trovato al percorso specificato: {percorsoFile}");
        }
        catch (Exception ex)
        {
            logFile.LogError($"Errore generico durante la lettura del file CSV: {ex.Message}");
        }

        return datiComplessivi;
    }

    // Funzione 4: Calcolo dei metri cubi da assegnare in base ai giorni di validità e numero di componenti
    public static int? calcolaMC(int giorniBonus, int componenti)
    {
        if (giorniBonus <= 0 || componenti <= 0)
        {
            return null;
        }
        return (50 * giorniBonus * componenti) / 1000;
    }

}

// Vecchia Funzione che verifica solo i membri forniti dal INPS tramite i codici fiscali
// if (codiciFiscaliFamigliari.Length > 0)
// {
//     foreach (var codFisc in codiciFiscaliFamigliari)
//     {
//         var dichiaranteFamigliare = dichiaranti.Where(s => s.CodiceFiscale == codFisc && s.IdEnte == selectedEnteId).ToList();
//         DateTime dataNascita = dichiaranteFamigliare[0].DataNascita;
//         if (dichiaranteFamigliare.Count == 1 && (FunzioniTrasversali.CalcolaEta(dataNascita) >= 18)) // Verifico che il membro della famiglia esista e abbia almeno 18 anni
//         {
//             // Verifico se il membro della famiglia ha una fornitura idrica diretta
//             (string esitoFamigliare, int? idFornituraMembro) = FunzioniTrasversali.VerificaEsistenzaFornitura(codFisc, selectedEnteId, context, dichiaranteFamigliare[0].IndirizzoResidenza, dichiaranteFamigliare[0].NumeroCivico, dichiaranteFamigliare[0].Cognome, dichiaranteFamigliare[0].Nome, dichiaranteFamigliare[0].DataNascita);
//             idFornituraIdrica = idFornituraMembro;
//             if (esitoFamigliare == "01")
//             {
//                 esito = "01"; // Se uno dei membri della famiglia ha una fornitura diretta, l'esito è 01
//                 break;
//             }
//             else if (esitoFamigliare == "03")
//             {
//                 esito = "03";
//                 break;
//             }
//             else if (esitoFamigliare == "04")
//             {
//                 // 3.g) Verifico se Presenza_POD è SI
//                 if (presenzaPod.Equals("Si", StringComparison.OrdinalIgnoreCase))
//                 {
//                     esito = "02"; // Se nessun membro della famiglia ha una fornitura diretta, ma Presenza_POD è SI, l'esito è 02
//                 }
//             }
//         }
//     }