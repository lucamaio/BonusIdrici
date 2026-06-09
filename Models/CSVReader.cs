using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using Data;
using Controllers;

using leggiCSV;
using Org.BouncyCastle.Crypto.Digests;
using ZstdSharp.Unsafe;
using Org.BouncyCastle.Bcpg;
using Models;
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
    public static DatiCsvCompilati LoadAnagrafe(string percorsoFile, int selectedEnteId, ApplicationDbContext _context, int idUser, int annoRiferimento, int meseRiferimento)
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

                datiComplessivi.DichiarantiSnapshot.Add(CreaSnapshot(dichiarante, annoRiferimento, meseRiferimento));

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
    public static DatiCsvCompilati LeggiFileUtenzeIdriche(string percorsoFile, int selectedEnteId, ApplicationDbContext _context, int idUser, int annoRiferimento, int meseRiferimento)
    {
        // Parte 1: Creazione della variabile da restituire e apertura file di log

        var datiComplessivi = new DatiCsvCompilati();
        FileLog logFile = new FileLog($"wwwroot/log/Elaborazione_Utenze.log");  // Creo/Apro il file di log
        List<string> errori = new List<string>();
        List<string> warning = new List<string>();
        // Inizializzo una variabile per tenere traccia del numero di Indirizzi mal formati trovatti
        int countIndirizziMalFormati = 0;
        // int countVariazioneToponomi = 0;

        try
        {
            // Leggo il numero righe da processare escludendo l'intestazione

            var righeFile = File.ReadAllLines(percorsoFile);
            var intestazioni = righeFile.FirstOrDefault()?
                .Split(CsvDelimiter)
                .Select(NormalizzaIntestazioneCsv)
                .ToList() ?? new List<string>();
            var righe = righeFile.Skip(1);

            int IndiceIdAcquedotto = TrovaIndiceColonna(intestazioni, 0, "IDAcquedotto");
            int IndiceStato = TrovaIndiceColonna(intestazioni, 9, "Stato");
            int IndiceMatricolaContatore = TrovaIndiceColonna(intestazioni, 12, "MatContatore");
            int IndicePeriodoIniziale = TrovaIndiceColonna(intestazioni, 13, "PeriodoInizio");
            int IndicePeriodoFinale = TrovaIndiceColonna(intestazioni, 14, "PeriodoFine");
            int IndiceIndirizzoUbicazione = TrovaIndiceColonna(intestazioni, 16, "ToponimiUbiDescrizione");
            int IndiceNumeroCivico = TrovaIndiceColonna(intestazioni, 17, "NCivico");
            int IndiceSubUbicazione = TrovaIndiceColonna(intestazioni, 18, "Sub");
            int IndiceScalaUbicazione = TrovaIndiceColonna(intestazioni, 19, "Scala");
            int IndicePiano = TrovaIndiceColonna(intestazioni, 20, "Piano");
            int IndiceInterno = TrovaIndiceColonna(intestazioni, 21, "Interno");
            int IndiceTipoUtenza = TrovaIndiceColonna(intestazioni, 27, "CategoriaDDesCategoria", "TipoUtenzaDom");
            int IndiceCognome = TrovaIndiceColonna(intestazioni, 33, "AnagraficaCognome");
            int IndiceNome = TrovaIndiceColonna(intestazioni, 34, "AnagraficaNome");
            int IndiceSesso = TrovaIndiceColonna(intestazioni, 35, "Sesso");
            int IndiceDataNascita = TrovaIndiceColonna(intestazioni, 36, "DataNascita");
            int IndiceCodiceFiscale = TrovaIndiceColonna(intestazioni, 37, "CodiceFiscale");
            int IndicePartitaIva = TrovaIndiceColonna(intestazioni, 38, "PartitaIVA");

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

                if (string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[IndiceIdAcquedotto])))
                {
                    warning.Add($"Attenzione: Id Acquedotto mancante. Per la snapshot verra usata una chiave alternativa basata su CodiceFiscale, Matricola, Indirizzo e Civico. | Riga {rigaCorrente} | Nominativo: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCognome])} {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceNome])} | Codice Fiscale: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCodiceFiscale])}");
                }

                // d) Controllo se il campo Codice Fiscale è valido è != null

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCodiceFiscale])))
                {
                    // logFile.LogInfo($"SONO NULL! | SESSO: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSesso])}");
                    if (!FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSesso]).Equals("D", StringComparison.OrdinalIgnoreCase))
                    {
                        logFile.LogInfo("COD FISC MANCATE E sesso != D");
                        errori.Add($"Attenzione : Codice Fiscale mancante, saltata. | Riga {rigaCorrente} | Nominativo: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCognome])} {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceNome])}");
                        error = true;
                    }
                    else if (string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[IndicePartitaIva])) &&
                            FunzioniTrasversali.rimuoviVirgolette(campi[IndiceStato]) != "4" &&
                            FunzioniTrasversali.rimuoviVirgolette(campi[IndiceStato]) != "5" &&
                            string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[IndicePeriodoFinale])) &&
                            FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSesso]).Equals("D", StringComparison.OrdinalIgnoreCase))
                    {
                        warning.Add($"Attenzione: Codice Fiscale e Partita IVA della ditta {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCognome])} non presente. (Questo non è un errore, ma una segnalazione). | Riga {rigaCorrente}");
                        error = true;
                    }
                }
                else if (FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCodiceFiscale]).Length != 16)
                {
                    // logFile.LogInfo($"mal formata! | SESSO: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSesso])}");
                    if (!FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSesso]).Equals("D", StringComparison.OrdinalIgnoreCase))
                    {
                        // logFile.LogInfo("Sesso diverso da D!");
                        errori.Add($"Attenzione : Codice Fiscale mal formato, saltata. | Riga {rigaCorrente} | Nominativo: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCognome])} {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceNome])}");
                        error = true;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[IndicePartitaIva])) &&
                            FunzioniTrasversali.rimuoviVirgolette(campi[IndiceStato]) != "4" &&
                            FunzioniTrasversali.rimuoviVirgolette(campi[IndiceStato]) != "5" &&
                            string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[IndicePeriodoFinale])) &&
                            FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSesso]).Equals("D", StringComparison.OrdinalIgnoreCase))
                        {
                            // logFile.LogInfo("PIVA 2 NULL && stato != 4, e sesso =D");
                            warning.Add($"Attenzione: Codice Fiscale e Partita IVA della ditta {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCognome])} non trovati. (Questo non è un errore, ma una segnalazione). | Riga {rigaCorrente}");
                            error = true;
                        }
                    }
                }


                // e) Controllo se la matricola del contatore è presente

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[IndiceMatricolaContatore])) && FunzioniTrasversali.rimuoviVirgolette(campi[IndiceStato]) != "4" && FunzioniTrasversali.rimuoviVirgolette(campi[IndiceStato]) != "5" && string.IsNullOrEmpty(FunzioniTrasversali.rimuoviVirgolette(campi[IndicePeriodoFinale])))
                {

                    errori.Add($"Attenzione: Matricola Contatore mancante, saltata. Riga {rigaCorrente} | Nominativo: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCognome])} {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceNome])} | Codice Fiscale: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCodiceFiscale])}");
                    error = true;
                }

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[IndiceIdAcquedotto])))
                {
                    warning.Add($"Attenzione: Id Acquedotto mancante. Riga {rigaCorrente}  | Nominativo: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCognome])} {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceNome])} | Codice Fiscale: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCodiceFiscale])}");
                }

                // f) Controllo se i campi nomi, cognome e sesso sono presenti

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCognome])) && string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[IndiceNome])) && FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSesso]).ToUpper() != "D")
                {
                    errori.Add($"Attenzione: Nome o Cognome mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceIdAcquedotto])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceMatricolaContatore])}");
                    error = true;
                }

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSesso])) ||
                    (FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSesso]).ToUpper() != "M" && FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSesso]).ToUpper() != "F" && FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSesso]).ToUpper() != "D"))
                {
                    errori.Add($"Attenzione: Sesso mancante o mal formato, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceIdAcquedotto])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceMatricolaContatore])}");
                    error = true;
                }

                // g) Controllo se il campo periodoIniziale è presente

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[IndicePeriodoIniziale])))
                {
                    errori.Add($"Attenzione: Periodo iniziale mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceIdAcquedotto])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceMatricolaContatore])}");
                    error = true;
                }

                // h) Verifico se il campo tipo utenza è presente

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[IndiceTipoUtenza])))
                {
                    errori.Add($"Attenzione: Tipo Utenza mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceIdAcquedotto])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceMatricolaContatore])}");
                    error = true;
                }

                // i) Verifico se il campo indirizzo Ubicazione è presente

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[IndiceIndirizzoUbicazione])))
                {
                    errori.Add($"Attenzione: Indirizzo ubicazione mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceIdAcquedotto])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceMatricolaContatore])}");
                    error = true;
                }

                if (!FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSesso]).Equals("D", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[IndiceDataNascita])))
                {
                    warning.Add($"Attenzione: Data Nascita mancante. La riga viene comunque caricata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceIdAcquedotto])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceMatricolaContatore])}");
                }

                // j) Verifico se il campo numero civico è presente

                var indirizzoCompletoPerCivico = FunzioniTrasversali.rimuoviVirgolette(campi[IndiceIndirizzoUbicazione]).ToUpper();
                var numeroCivicoSeparato = FunzioniTrasversali.rimuoviVirgolette(campi[IndiceNumeroCivico]).ToUpper();
                var indirizzoSeparato = FunzioniTrasversali.ExtractToponimoAndCivico(indirizzoCompletoPerCivico, numeroCivicoSeparato);

                if (!string.IsNullOrWhiteSpace(indirizzoSeparato.CivicoEstratto))
                {
                    logFile.LogInfo($"Civico estratto da indirizzo: '{indirizzoCompletoPerCivico}' -> toponimo '{indirizzoSeparato.Toponimo}', civico '{indirizzoSeparato.CivicoEstratto}'.");
                }

                if (!string.IsNullOrWhiteSpace(indirizzoSeparato.CivicoEstratto) &&
                    !string.IsNullOrWhiteSpace(numeroCivicoSeparato) &&
                    FunzioniTrasversali.FormattaNumeroCivico(numeroCivicoSeparato) != indirizzoSeparato.CivicoEstratto)
                {
                    warning.Add($"Conflitto civico: indirizzo '{indirizzoCompletoPerCivico}' contiene civico '{indirizzoSeparato.CivicoEstratto}', ma il campo civico separato contiene '{FunzioniTrasversali.FormattaNumeroCivico(numeroCivicoSeparato)}'. | Riga {rigaCorrente}");
                    logFile.LogInfo($"Conflitto civico: indirizzo '{indirizzoCompletoPerCivico}' contiene civico '{indirizzoSeparato.CivicoEstratto}', ma il campo civico separato contiene '{FunzioniTrasversali.FormattaNumeroCivico(numeroCivicoSeparato)}'.");
                }

                if (string.IsNullOrWhiteSpace(indirizzoSeparato.NumeroCivico))
                {
                    errori.Add($"Attenzione: Numero civico mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceIdAcquedotto])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[IndiceMatricolaContatore])}");
                    error = true;
                }

                // Parte 3: Verifica se l'indirizzo di ubicazione è associabile ad un toponimo

                //  a) Variabili di supporto
                var cod_fisc = FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCodiceFiscale]).ToUpper();
                var indirizzoUbicazione = FunzioniTrasversali.rimuoviVirgolette(campi[IndiceIndirizzoUbicazione]).ToUpper();
                var indirizzoUbicazioneNormalizzato = FunzioniTrasversali.NormalizeToponimo(indirizzoUbicazione);
                var indirizzoFormattato = FunzioniTrasversali.FormattaIndirizzo(_context, indirizzoUbicazione, cod_fisc, selectedEnteId);
                
                string? indirizzoRicavato = indirizzoFormattato != null ? indirizzoFormattato.ToUpper() : null;

                // Mi ricavo il tipo di toponimo e l'intestazione base

                (string? tipoToponimo, string? intestazione) = FunzioniTrasversali.AnalizzaIndirizzoPerToponimo(indirizzoUbicazione);

                //logFile.LogDebug($"Riga {rigaCorrente} | Indirizzo Ubicazione: {indirizzoUbicazione} | Indirizzo Ubicazione: {indirizzoUbicazione} | Tipo Toponimo: {tipoToponimo} | Intestazione: {intestazione}");

                int? idToponimo = null;

                // b) Controllo nel DB se esiste già il toponimo
                /*
                 * Logica legacy mantenuta per fallback. La nuova normalizzazione
                 * deve usare VieEnte e IndirizziNormalizzati.
                 *
                 * Questo blocco continua a cercare Toponomi esistenti durante
                 * l'import utenze, cosi da mantenere attivo il comportamento storico.
                 */
                Toponimo? toponimoTrova = toponimi
                    .FirstOrDefault(s => s.denominazione == indirizzoUbicazione && s.IdEnte == selectedEnteId);

                if (false && toponimoTrova == null)
                {
                    var candidatiCompatibili = toponimi
                        .Where(s => s.IdEnte == selectedEnteId &&
                            (FunzioniTrasversali.AreToponimiCompatibili(s.denominazione, indirizzoUbicazione) ||
                             FunzioniTrasversali.AreToponimiCompatibili(s.normalizzazione, indirizzoUbicazione)))
                        .ToList();

                    if (candidatiCompatibili.Count == 1)
                    {
                        toponimoTrova = candidatiCompatibili[0];
                        logFile.LogInfo($"Toponimo compatibile per iniziale nome proprio: '{indirizzoUbicazione}' -> '{toponimoTrova.denominazione}'.");
                    }
                    else if (candidatiCompatibili.Count > 1)
                    {
                        warning.Add($"Toponimo non associato automaticamente per ambiguità iniziale nome proprio: '{indirizzoUbicazione}'. Candidati: {candidatiCompatibili.Count}. | Riga {rigaCorrente}");
                        logFile.LogInfo($"Toponimo non associato automaticamente per ambiguità iniziale nome proprio: '{indirizzoUbicazione}'. Candidati: {candidatiCompatibili.Count}.");
                    }
                }

                if (toponimoTrova != null)
                {
                    idToponimo = toponimoTrova.id;
                    if (!string.Equals(toponimoTrova.denominazione, indirizzoUbicazione, StringComparison.OrdinalIgnoreCase))
                    {
                        logFile.LogInfo($"Toponimo associato tramite normalizzazione: '{indirizzoUbicazione}' -> '{indirizzoUbicazioneNormalizzato}'. ID toponimo: {idToponimo}");
                    }
                    //logFile.LogDebug($"Riga {rigaCorrente} | Trovato toponimo ID: {idToponimo} per indirizzo: {indirizzoUbicazione}");

                    // c) Aggiorno solo se non è già normalizzato e se ho un indirizzo ricavato valido
                    if (toponimoTrova.normalizzazione == null && indirizzoRicavato != null)
                    {
                        toponimoTrova.normalizzazione = indirizzoRicavato;

                        // mi ricavo il tipo di toponimo e l'intestazione in forma normale

                        toponimoTrova.dataAggiornamento = DateTime.Now;
                        // Logica legacy mantenuta per fallback. La nuova normalizzazione deve usare VieEnte e IndirizziNormalizzati.
                        datiComplessivi.ToponimiDaAggiornare.Add(toponimoTrova);
                    }

                    // d) Verifico se il tipo di toponimo è cambiato e lo aggiorno
                    if (false && tipoToponimo != null && toponimoTrova.tipoToponimo != tipoToponimo)
                    {
                        toponimoTrova.tipoToponimo = tipoToponimo;
                        toponimoTrova.dataAggiornamento = DateTime.Now;
                        // Logica legacy mantenuta per fallback. La nuova normalizzazione deve usare VieEnte e IndirizziNormalizzati.
                        datiComplessivi.ToponimiDaAggiornare.Add(toponimoTrova);
                    }

                }
                else
                {
                    var toponimoLista = datiComplessivi.Toponimi?
                        .FirstOrDefault(t => t.denominazione == indirizzoUbicazione && t.IdEnte == selectedEnteId);

                    if (false && toponimoLista == null && datiComplessivi.Toponimi != null)
                    {
                        var candidatiCompatibiliLista = datiComplessivi.Toponimi
                            .Where(t => t.IdEnte == selectedEnteId &&
                                (FunzioniTrasversali.AreToponimiCompatibili(t.denominazione, indirizzoUbicazione) ||
                                 FunzioniTrasversali.AreToponimiCompatibili(t.normalizzazione, indirizzoUbicazione)))
                            .ToList();

                        if (candidatiCompatibiliLista.Count == 1)
                        {
                            toponimoLista = candidatiCompatibiliLista[0];
                            logFile.LogInfo($"Toponimo compatibile per iniziale nome proprio: '{indirizzoUbicazione}' -> '{toponimoLista.denominazione}'.");
                        }
                        else if (candidatiCompatibiliLista.Count > 1)
                        {
                            warning.Add($"Toponimo non associato automaticamente per ambiguità iniziale nome proprio: '{indirizzoUbicazione}'. Candidati in import: {candidatiCompatibiliLista.Count}. | Riga {rigaCorrente}");
                            logFile.LogInfo($"Toponimo non associato automaticamente per ambiguità iniziale nome proprio: '{indirizzoUbicazione}'. Candidati in import: {candidatiCompatibiliLista.Count}.");
                        }
                    }

                    if (toponimoLista != null)
                    {
                        // Caso: esiste già nella lista → aggiorno solo i campi mancanti
                        if (toponimoLista.normalizzazione == null && indirizzoRicavato != null)
                        {
                            toponimoLista.normalizzazione = indirizzoRicavato;
                            // mi ricavo il tipo di toponimo e l'intestazione in forma normale
                            toponimoLista.dataAggiornamento = DateTime.Now;
                        }

                        if (false && tipoToponimo != null && toponimoLista.tipoToponimo != tipoToponimo)
                        {
                            toponimoLista.tipoToponimo = tipoToponimo;
                            toponimoLista.dataAggiornamento = DateTime.Now;
                        }

                    }
                    else
                    {
                        // Caso: completamente nuovo → lo aggiungo alla lista
                        /*
                         * Logica legacy mantenuta per fallback. La nuova normalizzazione
                         * deve usare VieEnte e IndirizziNormalizzati.
                         *
                         * La creazione del Toponimo resta attiva per compatibilita
                         * con il flusso precedente dell'import utenze.
                         */
                        var nuovoToponimo = new Toponimo
                        {
                            denominazione = indirizzoUbicazione,
                            IdEnte = selectedEnteId,
                            dataCreazione = DateTime.Now,
                            normalizzazione = indirizzoRicavato // valorizzo subito se disponibile
                        };

                        datiComplessivi.Toponimi?.Add(nuovoToponimo);
                    }
                }

                // Parte 4: Mi ricavo l'id del dichiarante associato all'utenza

                var cognome = FunzioniTrasversali.rimuoviVirgolette(campi[IndiceCognome]).ToUpper();
                var nome = FunzioniTrasversali.rimuoviVirgolette(campi[IndiceNome]).ToUpper();

                var dichiaranteTrovato = _context.Dichiaranti.FirstOrDefault(d => d.Cognome == cognome && d.Nome == nome && d.CodiceFiscale == cod_fisc && d.IdEnte == selectedEnteId);
                int? idDichiarante = dichiaranteTrovato != null ? dichiaranteTrovato.id : (int?)null;

                // Parte 5: Controllo se sono presenti Errori

                if (error)
                {
                    continue; // Salta la riga se ci sono errori
                }

                // Creo un utenza

                var idAcquedottoImport = FunzioniTrasversali.rimuoviVirgolette(campi[IndiceIdAcquedotto]);
                if (string.IsNullOrWhiteSpace(idAcquedottoImport))
                {
                    idAcquedottoImport = CreaChiaveAlternativaUtenza(cod_fisc, FunzioniTrasversali.rimuoviVirgolette(campi[IndiceMatricolaContatore]), indirizzoUbicazione, FunzioniTrasversali.FormattaNumeroCivico(campi[IndiceNumeroCivico]));
                }

                var utenza = new UtenzaIdrica
                {
                    idAcquedotto = idAcquedottoImport,
                    stato = int.TryParse(FunzioniTrasversali.rimuoviVirgolette(campi[IndiceStato]), out int stato) ? stato : 0,
                    periodoIniziale = FunzioniTrasversali.ConvertiData(FunzioniTrasversali.rimuoviVirgolette(campi[IndicePeriodoIniziale])),
                    periodoFinale = FunzioniTrasversali.ConvertiData(FunzioniTrasversali.rimuoviVirgolette(campi[IndicePeriodoFinale])),
                    matricolaContatore = FunzioniTrasversali.rimuoviVirgolette(campi[IndiceMatricolaContatore]).ToUpper(),
                    indirizzoUbicazione = indirizzoUbicazione,
                    numeroCivico = FunzioniTrasversali.FormattaNumeroCivico(campi[IndiceNumeroCivico]).ToUpper(),
                    subUbicazione = FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSubUbicazione]).ToUpper(),
                    scalaUbicazione = FunzioniTrasversali.rimuoviVirgolette(campi[IndiceScalaUbicazione]),
                    piano = FunzioniTrasversali.rimuoviVirgolette(campi[IndicePiano]),
                    interno = FunzioniTrasversali.rimuoviVirgolette(campi[IndiceInterno]),
                    tipoUtenza = FunzioniTrasversali.rimuoviVirgolette(campi[IndiceTipoUtenza]).ToUpper(),
                    cognome = cognome,
                    nome = nome,
                    sesso = FunzioniTrasversali.rimuoviVirgolette(campi[IndiceSesso]).ToUpper(),
                    DataNascita = FunzioniTrasversali.ConvertiData(FunzioniTrasversali.rimuoviVirgolette(campi[IndiceDataNascita])),
                    codiceFiscale = cod_fisc,
                    partitaIva = FunzioniTrasversali.rimuoviVirgolette(campi[IndicePartitaIva]),
                    data_creazione = DateTime.Now,
                    IdEnte = selectedEnteId,
                    idToponimo = idToponimo,
                    IdUser = idUser,
                    IdDichiarante = idDichiarante,
                };

                /*
                 * Logica legacy mantenuta per fallback. La nuova normalizzazione
                 * deve usare VieEnte e IndirizziNormalizzati.
                 *
                 * Il collegamento idToponimo sull'utenza resta attivo per non
                 * rompere report e controlli che dipendono dalla tabella Toponomi.
                 */
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

                if (!datiComplessivi.UtenzeIdricheSnapshot.Any(s =>
                        s.IdEnte == selectedEnteId
                        && s.AnnoRiferimento == annoRiferimento
                        && s.MeseRiferimento == meseRiferimento
                        && s.IdAcquedotto == utenza.idAcquedotto))
                {
                    datiComplessivi.UtenzeIdricheSnapshot.Add(CreaSnapshotUtenza(utenza, utenzaEsistente?.id, annoRiferimento, meseRiferimento));
                }

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

                    /*
                     * Logica legacy mantenuta per fallback. La nuova normalizzazione
                     * deve usare VieEnte e IndirizziNormalizzati.
                     *
                     * L'aggiornamento dell'idToponimo resta attivo per compatibilita
                     * con la precedente normalizzazione basata su Toponomi.
                     */
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

    // Funzione 3: Legge il file CSV proveniente da INPS per generare i domande delle persone con bonus idrico
    public static DatiCsvCompilati LeggiFileINPS(string percorsoFile, ApplicationDbContext context, int selectedEnteId, int idReport, bool confrontoCivico, bool escludiComponenti, bool escludiAlertSnapshot, int annoReport, int meseReport)
    {
        // Parte 1: Inizializzazione delle variabili
        var datiComplessivi = new DatiCsvCompilati();
        FileLog logFile = new FileLog($"wwwroot/log/Elaborazione_INPS.log");
        List<string> errori = new List<string>();
        DateTime dataElaborazione = DateTime.Now; // Neccessaria per impostarla nei domande ed evitare divisioni
        try
        {
            var righe = File.ReadAllLines(percorsoFile).Skip(1);
            var inizioMeseReport = new DateTime(annoReport, meseReport, 1);
            var snapshotPeriodo = context.DichiarantiSnapshot
                .Where(s => s.IdEnte == selectedEnteId
                    && s.AnnoRiferimento == annoReport
                    && s.MeseRiferimento == meseReport)
                .ToList();
            bool snapshotDisponibile = snapshotPeriodo.Count > 0;
            const string avvisoSnapshotAssente = "Elaborazione effettuata senza snapshot anagrafica del mese/anno di riferimento. Verificare numero componenti e nucleo familiare.";
            bool snapshotUtenzeDisponibile = context.UtenzeIdricheSnapshot.Any(s =>
                s.IdEnte == selectedEnteId
                && s.AnnoRiferimento == annoReport
                && s.MeseRiferimento == meseReport);
            const string avvisoSnapshotUtenzeAssente = "Snapshot utenze non disponibile per il mese/anno del report. La verifica della fornitura è stata effettuata sui dati correnti.";

            var avvisiSnapshotReport = new List<string>();
            if (!snapshotDisponibile)
            {
                avvisiSnapshotReport.Add(avvisoSnapshotAssente);
            }
            if (!snapshotUtenzeDisponibile)
            {
                avvisiSnapshotReport.Add(avvisoSnapshotUtenzeAssente);
            }
            var report = context.Reports.FirstOrDefault(r => r.id == idReport);
            if (report != null)
            {
                foreach (var avvisoSnapshotReport in avvisiSnapshotReport)
                {
                    if (string.IsNullOrWhiteSpace(report.note))
                    {
                        report.note = avvisoSnapshotReport;
                    }
                    else if (!report.note.Contains(avvisoSnapshotReport, StringComparison.OrdinalIgnoreCase))
                    {
                        report.note = report.note + "\n" + avvisoSnapshotReport;
                    }
                }
            }

            int rigaCorrente = 1;
            logFile.LogInfo($"Numero di righe da elaborare: {righe.Count()}");
            if (!snapshotDisponibile)
            {
                logFile.LogWarning($"Nessuna snapshot anagrafica trovata per ente {selectedEnteId}, periodo {meseReport:D2}/{annoReport}. Uso anagrafe corrente come fallback.");
            }
            if (!snapshotUtenzeDisponibile)
            {
                logFile.LogWarning($"Nessuna snapshot utenze trovata per ente {selectedEnteId}, periodo {meseReport:D2}/{annoReport}. Uso utenze correnti come fallback.");
            }
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
                    errori.Add(FormatInpsLogMessage(rigaCorrente, null, null, "TRACCIATO", $"riga malformata, saltata. Numero di campi: {campi.Length}. Attesi almeno 16."));
                    continue;
                }

                string idAtoCsv = NormalizeCsvValue(campi[0]);
                string codiceBonusCsv = NormalizeCsvValue(campi[1]).ToUpperInvariant();
                string codiceFiscaleCsv = NormalizeCsvValue(campi[2]).ToUpperInvariant();
                string annoValiditaCsv = NormalizeCsvValue(campi[6]);
                string dataInizioCsv = NormalizeCsvValue(campi[7]);
                string dataFineCsv = NormalizeCsvValue(campi[8]);
                string istatCsv = NormalizeCsvValue(campi[11]).ToUpperInvariant();
                string capCsv = NormalizeCsvValue(campi[12]).ToUpperInvariant();
                string provinciaCsv = NormalizeCsvValue(campi[13]).ToUpperInvariant();
                string presenzaPodCsv = NormalizeSiNo(campi[14]);
                string numeroComponentiCsv = NormalizeCsvValue(campi[15]);
                DateTime dataInizioValiditaParsed;
                DateTime dataFineValiditaParsed;
                string[] codiciFiscaliFamigliariNormalizzati = Array.Empty<string>();

                // Verifico se il campo idAto è presente e nonn vuoto
                if (string.IsNullOrWhiteSpace(idAtoCsv))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "ID_ATO", "campo mancante."));
                    error = true;
                }
                else if (!IsNumeric(idAtoCsv))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "ID_ATO", "non numerico."));
                    error = true;
                }
                else if (!IsMaxLength(idAtoCsv, 4))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "ID_ATO", "lunghezza superiore a 4 caratteri."));
                    error = true;
                }

                // Verifico se il campo codice_bonus è presente, non vuoto è ha una lunhezza di 15 carratteri

                if (string.IsNullOrWhiteSpace(codiceBonusCsv))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "COD_BONUS_IDRICO", "campo mancante."));
                    error = true;
                }
                else if (!IsLength(codiceBonusCsv, 15))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "COD_BONUS_IDRICO", "lunghezza diversa da 15 caratteri."));
                    error = true;
                }

                // Verifico se il campo Codice Fiscale è presente e non vuoto

                if (!string.IsNullOrWhiteSpace(codiceFiscaleCsv) && !IsValidCodiceFiscaleLength(codiceFiscaleCsv))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "CF_DICHIARANTE", "lunghezza diversa da 16 caratteri."));
                    error = true;
                }

                // Verifico se il campo NOME_DICHIARANTE e COGNOME_DICHIARANTE sono presenti e non vuoti
                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[3])) || string.IsNullOrWhiteSpace(FunzioniTrasversali.rimuoviVirgolette(campi[4])))
                {
                    errori.Add($"Attenzione: Nome o Cognome mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo Anno_validità è presente e non vuoto

                if (string.IsNullOrWhiteSpace(annoValiditaCsv))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "ANNO_VALIDITA", "campo mancante."));
                    error = true;
                }

                // verifico se il campo Data_inizio_validità è presente e non vuoto
                else if (!IsNumeric(annoValiditaCsv))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "ANNO_VALIDITA", "non numerico."));
                    error = true;
                }
                else if (!IsLength(annoValiditaCsv, 4))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "ANNO_VALIDITA", "lunghezza diversa da 4 caratteri."));
                    error = true;
                }

                if (!IsValidDateDdMmYyyy(dataInizioCsv, out dataInizioValiditaParsed))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "DATA_INIZIO", "non valida. Formato atteso GG/MM/AAAA."));
                    error = true;
                }

                // Verifico se il campo Data_fine_validità è presente e non vuoto
                if (!IsValidDateDdMmYyyy(dataFineCsv, out dataFineValiditaParsed))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "DATA_FINE", "non valida. Formato atteso GG/MM/AAAA."));
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
                string istat = istatCsv;

                if (string.IsNullOrWhiteSpace(istatCsv) || !IsNumeric(istatCsv) || !IsLength(istatCsv, 6))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "ISTAT", "campo mancante, non numerico o lunghezza diversa da 6 caratteri."));
                    error = true;
                }

                // VERIFICO Se il campo cap è presente e non vuoto
                if (string.IsNullOrWhiteSpace(capCsv) || !IsNumeric(capCsv) || !IsLength(capCsv, 5))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "CAP_ABITAZIONE", "campo mancante, non numerico o lunghezza diversa da 5 caratteri."));
                    error = true;
                }

                // vrifico se il campo provincia_abitazione è presente e non vuoto
                if (string.IsNullOrWhiteSpace(provinciaCsv) || !IsLength(provinciaCsv, 2))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "PROVINCIA_ABITAZIONE", "campo mancante o lunghezza diversa da 2 caratteri."));
                    error = true;
                }

                // Verifico se il campo Presenza_POD è presente ed è valido
                if (string.IsNullOrWhiteSpace(presenzaPodCsv))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "PRESENZA_POD", "campo mancante o valore diverso da SI/NO."));
                    error = true;
                }

                // verifdico se il campo n_componenti è presente e non vuoto
                if (string.IsNullOrWhiteSpace(numeroComponentiCsv) || !IsNumeric(numeroComponentiCsv) || !IsMaxLength(numeroComponentiCsv, 2) || !int.TryParse(numeroComponentiCsv, out var numeroComponentiValidato) || numeroComponentiValidato <= 0)
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "NUMERO_COMPONENTI", "campo mancante, non numerico, lunghezza superiore a 2 caratteri o valore non maggiore di 0."));
                    error = true;
                }

                if (!ValidateCodiciFiscaliComponenti(campi[5], out codiciFiscaliFamigliariNormalizzati, out string? erroreCodiciFamigliari))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonusCsv, codiceFiscaleCsv, "CF_COMPONENTI", erroreCodiciFamigliari ?? "lista codici fiscali non valida."));
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
                string? idAto = idAtoCsv;
                string? codiceBonus = codiceBonusCsv;
                string? codiceFiscale = codiceFiscaleCsv;

                string? nomeDichiarante = NormalizeCsvValue(campi[3]).ToUpperInvariant();
                string? cognomeDichiarante = NormalizeCsvValue(campi[4]).ToUpperInvariant();
                string[]? codiciFiscaliFamigliari = codiciFiscaliFamigliariNormalizzati;

                string? annoValidita = annoValiditaCsv;
                DateTime dataInizioValidita = dataInizioValiditaParsed;
                DateTime dataFineValidita = dataFineValiditaParsed;

                string? indirizzoAbitazione = NormalizeCsvValue(campi[9]).ToUpperInvariant();
                string? numeroCivico = FunzioniTrasversali.FormattaNumeroCivico(campi[10]).ToUpper();
                string? istatAbitazione = istatCsv;
                string? capAbitazione = capCsv;
                string? provinciaAbitazione = provinciaCsv;

                string presenzaPod = presenzaPodCsv;
                string numeroComponenti_str = numeroComponentiCsv;
                int numeroComponenti = int.Parse(numeroComponenti_str);
                // logFile.LogInfo("Ho istanziato tutti i campi con i dati del CSV");

                //DateTime dataCreazione = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0); // Escludo i secondi della data attuale per evitare problemi successivi
                // logFile.LogInfo($"Ho inizializatto la data di creazione: {dataCreazione}");

                // 1.c) Aggiungo dei campi aggiuntivi neccessari per la creazione del domande
                string esitoStr = "No";
                string esito = "04"; // 01 = fornitura diretta, 02 = fornitura indiretta, 03 = fornitura diretta non rispetta requisiti, 04 =  fornitura indiretta non rispetta requisiti 
                int? idFornituraIdrica = null;
                string? note = null;
                bool verificare = false;
                if (!snapshotDisponibile && !escludiAlertSnapshot)
                {
                    note = avvisoSnapshotAssente;
                }
                if (!snapshotUtenzeDisponibile && !escludiAlertSnapshot)
                {
                    note = (note ?? string.Empty) + "\n" + avvisoSnapshotUtenzeAssente;
                    verificare = true;
                }

                // 2.a) mi salvo i campi relativi all'ente in modo da poter effetuare le operazioni successive

                var ente = context.Enti.FirstOrDefault(d => d.id == selectedEnteId);

                if (ente == null)
                {
                    errori.Add($"Attenzione: Nessun ente trovato con l'ID selezionato {selectedEnteId}. Riga {rigaCorrente}");
                    continue; // Salta la riga se non c'è l'ente
                }

                var codiceFiscaleEnte = ente.CodiceFiscale;
                var istatEnte = ente.istat;
                var capEnte = ente.Cap;

                string? messaggio = null;
                int? mc = null;
                int? idDichiarante = null;
                string? codiceFiscaleDichiarateTrovato = null;
                int? idUtenza = null;
                // 2.b) Verifico se i campi ISTAT, CAP e provincia corrispondono a l'ente selezionato il quale gestisce le utenze idriche
                // logFile.LogInfo($"Riga: {rigaCorrente} sto verificando l'indirizzo!");
                if (!(istatEnte != istatAbitazione || capEnte != capAbitazione || provinciaAbitazione != ente.Provincia))
                {
                    // 2.c) se i campi corispondono verifico se il richiedente è residente nel comune selezionato
                    var snapshotDichiarante = snapshotPeriodo.FirstOrDefault(s => s.CodiceFiscale == codiceFiscale);
                    bool usaSnapshot = snapshotDichiarante != null;
                    var dichiaranteCorrente = context.Dichiaranti.FirstOrDefault(s => s.CodiceFiscale == codiceFiscale && s.IdEnte == selectedEnteId);
                    var dichiaranteTrovato = usaSnapshot
                        ? CreaDichiaranteDaSnapshot(snapshotDichiarante!)
                        : dichiaranteCorrente;

                    if (snapshotDisponibile && snapshotDichiarante == null)
                    {
                        note = (note ?? string.Empty) + $"\nAttenzione: snapshot anagrafica assente per il codice fiscale {codiceFiscale} nel periodo {meseReport:D2}/{annoReport}. Usata anagrafe corrente come fallback.";
                        verificare = true;
                        logFile.LogWarning($"Snapshot assente per CF {codiceFiscale}, ente {selectedEnteId}, periodo {meseReport:D2}/{annoReport}. Uso anagrafe corrente come fallback.");
                    }
                    if (dichiaranteTrovato != null)
                    {
                        // logFile.LogInfo($"Ho trovato un dichiarante: {dichiaranteTrovato.ToString()}");
                        // 2.c.1) se è residente nel comune selzionato allora esito è uguale a Si
                        esitoStr = "Si";
                        idDichiarante = dichiaranteCorrente?.id;
                        //3.a) verifica se il richiedente ha una fornitura idrica diretta 

                        // logFile.LogInfo("Pre verifica esistenza fornitura");
                        (string esitoRestituito, int? idFornituraTrovato, string? messagge, int? idUtenzaDichiarante) = FunzioniTrasversali.VerificaEsistenzaFornitura(dichiaranteTrovato, selectedEnteId, context, indirizzoAbitazione, numeroCivico, confrontoCivico, annoReport, meseReport, true);
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
                            int numeroComponentiAnagrafico = usaSnapshot
                                ? CalcolaNumeroComponentiStorico(snapshotPeriodo, snapshotDichiarante!, inizioMeseReport)
                                : dichiaranteTrovato.NumeroComponenti;

                            if (numeroComponentiAnagrafico > 1)
                            {
                                // logFile.LogInfo("Cerco i Famigliari!");
                                // Cerco tra i famigliari
                                var today = DateTime.Today;
                                var cutoff = today.AddYears(-18);

                                var famigliari = usaSnapshot
                                    ? TrovaFamigliariSnapshot(snapshotPeriodo, snapshotDichiarante!, inizioMeseReport, cutoff)
                                        .Select(CreaDichiaranteDaSnapshot)
                                        .ToList()
                                    : context.Dichiaranti
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
                                    bool trovatoEsito01Famigliare = false;
                                    bool trovatoEsito03Famigliare = false;
                                    int? idUtenzaFallback03 = null;
                                    int? idFornituraFallback03 = null;
                                    string? codiceFiscaleFallback03 = null;
                                    foreach (var membro in famigliari)
                                    {
                                        // logFile.LogInfo($"Verifica 2 Esistenza Fornitura. Membro: {membro.ToString()}");

                                        (string esitoFamigliare, int? idFornituraMembro, string? messaggeFamigliare, int? idUtenzaMembro) = FunzioniTrasversali.VerificaEsistenzaFornitura(membro, selectedEnteId, context, indirizzoAbitazione, numeroCivico, confrontoCivico, annoReport, meseReport, true);
                                        // logFile.LogInfo($"Post pre verifica fornitura 2. Dati restituiti. Esito: {esitoRestituito} | id Fornitura: {idFornituraTrovato} | id Utenza {idUtenza} | Messaggio:\n {messagge}");

                                        if (messaggeFamigliare != "Nessuna fornitura trovata per il dichiarante.")
                                        {
                                            note = note + messaggeFamigliare;
                                        }

                                        if (esitoFamigliare == "01")
                                        {
                                            idUtenza = idUtenzaMembro;
                                            idFornituraIdrica = idFornituraMembro;
                                            codiceFiscaleDichiarateTrovato = membro.CodiceFiscale;
                                            note = null;
                                            esito = "01"; // Se uno dei membri della famiglia ha una fornitura diretta, l'esito è 01
                                            trovatoEsito01Famigliare = true;
                                            break;
                                        }
                                        else if (esitoFamigliare == "03")
                                        {
                                            if (!trovatoEsito03Famigliare)
                                            {
                                                trovatoEsito03Famigliare = true;
                                                idUtenzaFallback03 = idUtenzaMembro;
                                                idFornituraFallback03 = idFornituraMembro;
                                                codiceFiscaleFallback03 = membro.CodiceFiscale;
                                            }
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

                                    if (!trovatoEsito01Famigliare && trovatoEsito03Famigliare)
                                    {
                                        esito = "03";
                                        idUtenza = idUtenzaFallback03;
                                        idFornituraIdrica = idFornituraFallback03;
                                        codiceFiscaleDichiarateTrovato = codiceFiscaleFallback03;
                                    }
                                    else if (!trovatoEsito01Famigliare && presenzaPod.Equals("SI", StringComparison.OrdinalIgnoreCase))
                                    {
                                        esito = "02";
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
                        int numeroComponentiConfronto = usaSnapshot
                            ? CalcolaNumeroComponentiStorico(snapshotPeriodo, snapshotDichiarante!, inizioMeseReport)
                            : dichiaranteTrovato.NumeroComponenti;

                        if (numeroComponentiConfronto != numeroComponenti && !escludiComponenti)
                        {
                            if (ente.Selene == true)
                            {
                                note = note + $"\nAttenzione: Il numero di componenti fornito ({numeroComponenti}) non corrisponde a quello effettivo ({numeroComponentiConfronto}). é stato impostato come valore quello ricavato dal anagrafe.";
                                numeroComponenti = numeroComponentiConfronto;
                            }
                            else
                            {
                                note = note + $"\nAttenzione: Il numero di componenti fornito ({numeroComponenti}) non corrisponde a quello effettivo ({numeroComponentiConfronto}).";
                            }
                            verificare = true;
                            logFile.LogWarning($"Attenzione: Il numero di componenti fornito ({numeroComponenti}) non corrisponde a quello effettivo ({numeroComponentiConfronto}). Codice Bonus: {codiceBonus} | Codice Fiscale: {codiceFiscale}");
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

                if (!IsValidEsitoBonus(esito))
                {
                    errori.Add(FormatInpsLogMessage(rigaCorrente, codiceBonus, codiceFiscale, "ESITO", $"valore non valido '{esito}'. Impostato a 04."));
                    esito = "04";
                    verificare = true;
                    note = (note ?? string.Empty) + "\nAttenzione: esito non valido normalizzato a 04.";
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
                // int valueSerie;
                // if (serie == null)
                // {
                //     valueSerie = context.Enti.Where(s => s.id == selectedEnteId).Select(s => s.Serie).FirstOrDefault();
                // }
                // else
                // {
                //     valueSerie = (int)serie;
                // }

                // logFile.LogInfo("Sto creando il domande");

                // 4) Creo un nuovo domande con i dati raccolti
                var domanda = new Domanda
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
                    idReport = idReport,
                    //serie = valueSerie,
                    mc = mc,
                    incongruenze = verificare,
                    note = note != null ? note.Trim() : null,
                };

                // logFile.LogInfo($"Domande Creato riga: {rigaCorrente}: {domanda.ToString()}");

                // Parte 6: Verifico se il domande è gia presente

                 var domandaEsistente = context.Domande.FirstOrDefault(r => r.codiceBonus == domanda.codiceBonus && r.idReport == idReport);

                if (domandaEsistente == null)
                {
                    // logFile.LogInfo("Il domande non esiste");
                    datiComplessivi.domande.Add(domanda);
                }
                else
                {
                    // logFile.LogInfo("Sto Verificando se devo aggiornare i dati");
                    // Domande esistente, verifico se ci sono campi da aggiornare
                    bool aggiornare = false;

                    // Inzio - Confronto tra i dati del db e quelli del csv
                    // Verifico campo per campo se ci sono differenze per il campo idAto
                    if (domanda.idAto != null && domandaEsistente.idAto != null && domandaEsistente.idAto != domanda.idAto)
                    {
                        domandaEsistente.idAto = domanda.idAto;
                        aggiornare = true;
                    }

                    // Verifico campo per campo se ci sono differenze per il campo codiceFiscaleRichiedente

                    if (domanda.codiceFiscaleRichiedente != null && domandaEsistente.codiceFiscaleRichiedente != null && domandaEsistente.codiceFiscaleRichiedente != domanda.codiceFiscaleRichiedente)
                    {
                        domandaEsistente.codiceFiscaleRichiedente = domanda.codiceFiscaleRichiedente;
                        aggiornare = true;
                    }

                    // Verifico il campo numeroComponenti
                    if (domandaEsistente.numeroComponenti != domanda.numeroComponenti)
                    {
                        domandaEsistente.numeroComponenti = domanda.numeroComponenti;
                        aggiornare = true;
                    }
                    

                    // Verifico il campo codiceFiscaleUtenzaTrovata
                    if (domandaEsistente.codiceFiscaleUtenzaTrovata != domanda.codiceFiscaleUtenzaTrovata)
                    {
                        domandaEsistente.codiceFiscaleUtenzaTrovata = domanda.codiceFiscaleUtenzaTrovata;
                        aggiornare = true;
                    }

                    // Verifico campo per campo se ci sono differenze per il campo esito

                    if (domanda.esito != null && domandaEsistente.esito != null && domandaEsistente.esito != domanda.esito)
                    {
                        domandaEsistente.esito = domanda.esito;
                        aggiornare = true;
                    }

                    // Verifico campo per campo se ci sono differenze per il campo esitoStr

                    if (domanda.esitoStr != null && domandaEsistente.esitoStr != null && domandaEsistente.esitoStr != domanda.esitoStr)
                    {
                        domandaEsistente.esitoStr = domanda.esitoStr;
                        aggiornare = true;
                    }

                    // Verifico campo per campo se ci sono differenze per il campo idFornitura

                    if (domanda.idFornitura != null && domandaEsistente.idFornitura != null && domandaEsistente.idFornitura != domanda.idFornitura)
                    {
                        domandaEsistente.idFornitura = domanda.idFornitura;
                        aggiornare = true;
                    }

                    // Verifico campo per campo se ci sono differenze per il campo mc

                    if (domandaEsistente.mc != domanda.mc)
                    {
                        domandaEsistente.mc = domanda.mc;
                        aggiornare = true;
                    }

                    // Verifico campo per campo se ci sono differenze per il campo inizioValidita

                    // if (domandaEsistente.dataInizioValidita != domanda.dataInizioValidita)
                    // {
                    //     domandaEsistente.dataInizioValidita = domanda.dataInizioValidita;
                    //     aggiornare = true;
                    // }

                    // Verifico campo per campo se ci sono differenze per il campo fineValidita

                    // if (domandaEsistente.dataFineValidita != domanda.dataFineValidita)
                    // {
                    //     domandaEsistente.dataFineValidita = domanda.dataFineValidita;
                    //     aggiornare = true;
                    // }


                    // Verifico se il campo incongruenze è diverso
                    if (domanda.incongruenze != domandaEsistente.incongruenze)
                    {
                        domandaEsistente.incongruenze = domanda.incongruenze;
                        aggiornare = true;
                    }

                    // Verifico se il campo note è diverso
                    if (domandaEsistente.note != domanda.note)
                    {
                        domandaEsistente.note = domanda.note;
                        aggiornare = true;
                    }

                    // Verifico il campo idDichiarante

                    if (domanda.idDichiarante != domandaEsistente.idDichiarante)
                    {
                        domandaEsistente.idDichiarante = domanda.idDichiarante;
                        aggiornare = true;
                    }

                    // Verifico il campo idUtenza

                    if (domanda.idUtenza != domandaEsistente.idUtenza)
                    {
                        domandaEsistente.idUtenza = domanda.idUtenza;
                        aggiornare = true;
                    }


                    // Verifico se devo aggiornare dei dati

                    if (aggiornare)
                    {
                        // logFile.LogInfo("Aggiorno i dati e imposto la data d'aggiornamento");
                        // domandaEsistente.DataAggiornamento = DateTime.Now;
                        datiComplessivi.domandeDaAggiornare.Add(domandaEsistente);
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

    private static int TrovaIndiceColonna(IReadOnlyList<string> intestazioni, int indiceFallback, params string[] nomiColonna)
    {
        foreach (var nomeColonna in nomiColonna)
        {
            var nomeNormalizzato = NormalizzaIntestazioneCsv(nomeColonna);
            for (int i = 0; i < intestazioni.Count; i++)
            {
                if (string.Equals(intestazioni[i], nomeNormalizzato, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return indiceFallback;
    }

    private static string NormalizzaIntestazioneCsv(string? intestazione)
    {
        return FunzioniTrasversali.rimuoviVirgolette(intestazione)
            .Trim()
            .Replace(" ", string.Empty)
            .ToUpperInvariant();
    }

    private static DichiaranteSnapshot CreaSnapshot(Dichiarante dichiarante, int annoRiferimento, int meseRiferimento)
    {
        var snapshot = new DichiaranteSnapshot
        {
            IdEnte = dichiarante.IdEnte,
            IdUser = dichiarante.IdUser,
            AnnoRiferimento = annoRiferimento,
            MeseRiferimento = meseRiferimento,
            CodiceFiscale = dichiarante.CodiceFiscale,
            Cognome = dichiarante.Cognome,
            Nome = dichiarante.Nome,
            Sesso = dichiarante.Sesso,
            DataNascita = dichiarante.DataNascita,
            ComuneNascita = dichiarante.ComuneNascita,
            IndirizzoResidenza = dichiarante.IndirizzoResidenza,
            NumeroCivico = dichiarante.NumeroCivico,
            Parentela = dichiarante.Parentela,
            CodiceFamiglia = dichiarante.CodiceFamiglia,
            CodiceAbitante = dichiarante.CodiceAbitante,
            NumeroComponenti = dichiarante.NumeroComponenti,
            CodiceFiscaleIntestatarioScheda = dichiarante.CodiceFiscaleIntestatarioScheda,
            DataCancellazione = dichiarante.data_cancellazione,
            DataImportazione = DateTime.Now,
            HashRecord = string.Empty
        };

        snapshot.HashRecord = CalcolaHashSnapshot(snapshot);
        return snapshot;
    }

    private static string CalcolaHashSnapshot(DichiaranteSnapshot snapshot)
    {
        string raw = string.Join("|",
            snapshot.IdEnte,
            snapshot.AnnoRiferimento,
            snapshot.MeseRiferimento,
            snapshot.CodiceFiscale,
            snapshot.Cognome,
            snapshot.Nome,
            snapshot.Sesso,
            snapshot.DataNascita.ToString("O"),
            snapshot.ComuneNascita,
            snapshot.IndirizzoResidenza,
            snapshot.NumeroCivico,
            snapshot.Parentela,
            snapshot.CodiceFamiglia,
            snapshot.CodiceAbitante,
            snapshot.NumeroComponenti,
            snapshot.CodiceFiscaleIntestatarioScheda,
            snapshot.DataCancellazione?.ToString("O"));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    private static Dichiarante CreaDichiaranteDaSnapshot(DichiaranteSnapshot snapshot)
    {
        return new Dichiarante
        {
            id = null,
            Cognome = snapshot.Cognome,
            Nome = snapshot.Nome,
            CodiceFiscale = snapshot.CodiceFiscale,
            Sesso = snapshot.Sesso,
            DataNascita = snapshot.DataNascita,
            ComuneNascita = snapshot.ComuneNascita,
            IndirizzoResidenza = snapshot.IndirizzoResidenza,
            NumeroCivico = snapshot.NumeroCivico,
            CodiceAbitante = snapshot.CodiceAbitante,
            CodiceFamiglia = snapshot.CodiceFamiglia,
            Parentela = snapshot.Parentela,
            CodiceFiscaleIntestatarioScheda = snapshot.CodiceFiscaleIntestatarioScheda,
            NumeroComponenti = snapshot.NumeroComponenti,
            IdEnte = snapshot.IdEnte,
            IdUser = snapshot.IdUser,
            data_cancellazione = snapshot.DataCancellazione
        };
    }

    private static int CalcolaNumeroComponentiStorico(List<DichiaranteSnapshot> snapshotPeriodo, DichiaranteSnapshot dichiarante, DateTime inizioMeseReport)
    {
        var componenti = TrovaNucleoSnapshot(snapshotPeriodo, dichiarante, inizioMeseReport).Count;
        return componenti > 0 ? componenti : dichiarante.NumeroComponenti;
    }

    private static List<DichiaranteSnapshot> TrovaFamigliariSnapshot(List<DichiaranteSnapshot> snapshotPeriodo, DichiaranteSnapshot dichiarante, DateTime inizioMeseReport, DateTime maggiorenneDa)
    {
        return TrovaNucleoSnapshot(snapshotPeriodo, dichiarante, inizioMeseReport)
            .Where(s => s.CodiceFiscale != dichiarante.CodiceFiscale && s.DataNascita <= maggiorenneDa)
            .ToList();
    }

    private static List<DichiaranteSnapshot> TrovaNucleoSnapshot(List<DichiaranteSnapshot> snapshotPeriodo, DichiaranteSnapshot dichiarante, DateTime inizioMeseReport)
    {
        return snapshotPeriodo
            .Where(s =>
                s.IdEnte == dichiarante.IdEnte
                && !IsCancellatoPrimaDelPeriodo(s, inizioMeseReport)
                && (
                    (dichiarante.CodiceFamiglia.HasValue && s.CodiceFamiglia == dichiarante.CodiceFamiglia)
                    || (!string.IsNullOrWhiteSpace(dichiarante.CodiceFiscaleIntestatarioScheda)
                        && s.CodiceFiscaleIntestatarioScheda == dichiarante.CodiceFiscaleIntestatarioScheda)
                    || s.CodiceFiscale == dichiarante.CodiceFiscale
                ))
            .ToList();
    }

    private static bool IsCancellatoPrimaDelPeriodo(DichiaranteSnapshot snapshot, DateTime inizioMeseReport)
    {
        return snapshot.DataCancellazione.HasValue && snapshot.DataCancellazione.Value.Date < inizioMeseReport.Date;
    }

    private static UtenzaIdricaSnapshot CreaSnapshotUtenza(UtenzaIdrica utenza, int? idUtenzaOriginale, int annoRiferimento, int meseRiferimento)
    {
        var snapshot = new UtenzaIdricaSnapshot
        {
            IdEnte = utenza.IdEnte,
            IdUser = utenza.IdUser,
            AnnoRiferimento = annoRiferimento,
            MeseRiferimento = meseRiferimento,
            IdUtenzaOriginale = idUtenzaOriginale,
            IdAcquedotto = utenza.idAcquedotto,
            MatricolaContatore = utenza.matricolaContatore,
            Stato = utenza.stato,
            PeriodoIniziale = utenza.periodoIniziale,
            PeriodoFinale = utenza.periodoFinale,
            IndirizzoUbicazione = utenza.indirizzoUbicazione,
            NumeroCivico = utenza.numeroCivico,
            SubUbicazione = utenza.subUbicazione,
            ScalaUbicazione = utenza.scalaUbicazione,
            Piano = utenza.piano,
            Interno = utenza.interno,
            TipoUtenza = utenza.tipoUtenza,
            Cognome = utenza.cognome,
            Nome = utenza.nome,
            Sesso = utenza.sesso,
            DataNascita = utenza.DataNascita,
            CodiceFiscale = utenza.codiceFiscale,
            PartitaIva = utenza.partitaIva,
            IdToponimo = utenza.idToponimo,
            IdDichiarante = utenza.IdDichiarante,
            DataImportazione = DateTime.Now,
            HashRecord = string.Empty
        };

        snapshot.HashRecord = CalcolaHashSnapshotUtenza(snapshot);
        return snapshot;
    }

    private static string CreaChiaveAlternativaUtenza(string? codiceFiscale, string? matricola, string? indirizzo, string? civico)
    {
        var raw = string.Join("|", codiceFiscale, matricola, indirizzo, civico);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return "ALT-" + Convert.ToHexString(bytes)[..31];
    }

    private static string CalcolaHashSnapshotUtenza(UtenzaIdricaSnapshot snapshot)
    {
        string raw = string.Join("|",
            snapshot.IdEnte,
            snapshot.AnnoRiferimento,
            snapshot.MeseRiferimento,
            snapshot.IdUtenzaOriginale,
            snapshot.IdAcquedotto,
            snapshot.MatricolaContatore,
            snapshot.Stato,
            snapshot.PeriodoIniziale?.ToString("O"),
            snapshot.PeriodoFinale?.ToString("O"),
            snapshot.IndirizzoUbicazione,
            snapshot.NumeroCivico,
            snapshot.SubUbicazione,
            snapshot.ScalaUbicazione,
            snapshot.Piano,
            snapshot.Interno,
            snapshot.TipoUtenza,
            snapshot.Cognome,
            snapshot.Nome,
            snapshot.Sesso,
            snapshot.DataNascita?.ToString("O"),
            snapshot.CodiceFiscale,
            snapshot.PartitaIva,
            snapshot.IdToponimo,
            snapshot.IdDichiarante);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    private static bool IsNumeric(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit);
    }

    private static bool IsLength(string? value, int length)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length == length;
    }

    private static bool IsMaxLength(string? value, int maxLength)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length <= maxLength;
    }

    private static bool IsValidCodiceFiscaleLength(string? value)
    {
        return IsLength(NormalizeCsvValue(value), 16);
    }

    private static bool IsValidDateDdMmYyyy(string? value, out DateTime date)
    {
        return DateTime.TryParseExact(
            NormalizeCsvValue(value),
            "dd/MM/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    private static string NormalizeCsvValue(string? value)
    {
        return FunzioniTrasversali.rimuoviVirgolette(value ?? string.Empty).Trim();
    }

    private static string NormalizeSiNo(string? value)
    {
        var normalized = NormalizeCsvValue(value).ToUpperInvariant();
        return normalized == "SI" || normalized == "NO" ? normalized : string.Empty;
    }

    private static bool ValidateCodiciFiscaliComponenti(string? value, out string[] codiciFiscali, out string? errore)
    {
        errore = null;
        var normalized = NormalizeCsvValue(value);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            codiciFiscali = Array.Empty<string>();
            return true;
        }

        var codici = normalized
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(codice => codice.ToUpperInvariant())
            .ToArray();

        var codiceNonValido = codici.FirstOrDefault(codice => !IsValidCodiceFiscaleLength(codice));
        if (codiceNonValido != null)
        {
            codiciFiscali = codici.Where(IsValidCodiceFiscaleLength).ToArray();
            errore = $"codice fiscale {codiceNonValido} con lunghezza diversa da 16 caratteri.";
            return false;
        }

        codiciFiscali = codici;
        return true;
    }

    private static bool IsValidEsitoBonus(string? esito)
    {
        return esito is "01" or "02" or "03" or "04";
    }

    private static string FormatInpsLogMessage(int riga, string? codiceBonus, string? codiceFiscale, string campo, string motivo)
    {
        var parti = new List<string> { $"Riga {riga}" };

        if (!string.IsNullOrWhiteSpace(codiceBonus))
        {
            parti.Add($"COD_BONUS_IDRICO {codiceBonus}");
        }

        if (!string.IsNullOrWhiteSpace(codiceFiscale))
        {
            parti.Add($"CF_DICHIARANTE {codiceFiscale}");
        }

        parti.Add($"{campo} {motivo}");
        return string.Join(" - ", parti);
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
