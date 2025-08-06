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

public class CSVReader
{
    private const char CsvDelimiter = ';';

    public static DatiCsvCompilati LoadAnagrafe(string percorsoFile, int selectedEnteId, List<Dichiarante> dichiaranti)
    {
        var datiComplessivi = new DatiCsvCompilati();
        FileLog logFile = new FileLog($"wwwroot/log/Elaborazione_Anagrafe.log");
        List<string> errori = new List<string>();
        try
        {
            var righe = File.ReadAllLines(percorsoFile).Skip(1);
            int rigaCorrente = 1;
            logFile.LogInfo($"Nuovo caricamento dati Anagrafe ID ENTE: {selectedEnteId}");
            logFile.LogInfo($"Numero di righe da caricare: {righe.Count()}");

            foreach (var riga in righe)
            {
                rigaCorrente++;
                var error = false;

                // 1. VERIFICO CHE I CAMPI SONO VALIDI

                // a) verifico se la riga è vuota
                if (string.IsNullOrWhiteSpace(riga)) continue;

                var campi = riga.Split(CsvDelimiter);

                // b) Verifico che il file contine tutti i vampi minimi per poter procedere

                if (campi.Length < 19)
                {
                    errori.Add($"Attenzione: Riga {rigaCorrente} malformata, saltata. Numero di campi: {campi.Length}. Attesi almeno 39.");
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

                // Controllo se sono presenti Errori

                if (error)
                {
                    continue; // Salta la riga se ci sono errori
                }

                // 2) Creo una istanza di Dichiarante

                var dichiarante = new Dichiarante
                {
                    Cognome = FunzioniTrasversali.rimuoviVirgolette(campi[0]).ToUpper(),
                    Nome = FunzioniTrasversali.rimuoviVirgolette(campi[1]).ToUpper(),
                    CodiceFiscale = FunzioniTrasversali.rimuoviVirgolette(campi[2]).ToUpper(),
                    Sesso = FunzioniTrasversali.rimuoviVirgolette(campi[3]).ToUpper(),
                    DataNascita = FunzioniTrasversali.ConvertiData(campi[4]),
                    ComuneNascita = FunzioniTrasversali.rimuoviVirgolette(campi[5]).ToUpper(),
                    IndirizzoResidenza = FunzioniTrasversali.rimuoviVirgolette(campi[7]).ToUpper(),
                    NumeroCivico = FunzioniTrasversali.FormattaNumeroCivico(campi[8]),
                    Parentela = FunzioniTrasversali.rimuoviVirgolette(campi[10]).ToUpper(),
                    CodiceFamiglia = int.Parse(campi[11].Trim()),
                    NumeroComponenti = int.Parse(campi[13].Trim()),
                    CodiceFiscaleIntestatarioScheda = FunzioniTrasversali.rimuoviVirgolette(campi[19]),
                    IdEnte = selectedEnteId
                };

                var esiste = false;
                var aggiornare = false;

                if (dichiaranti != null && dichiarante != null)
                {
                    var dichiaranteEsistente = dichiaranti.Find(d => d.CodiceFiscale == dichiarante.CodiceFiscale);
                    if (dichiaranteEsistente != null)
                    {
                        esiste = true;
                        dichiarante.id = dichiaranteEsistente.id;
                        // Dichiarante già presente, controllo se devo aggiornare i campi
                        if (dichiarante.Nome != dichiaranteEsistente.Nome)
                        {
                            //dichiarante.Nome = dichiaranteEsistente.Nome;
                            aggiornare = true;
                        }

                        if (dichiarante.Cognome != dichiaranteEsistente.Cognome)
                        {
                            //dichiarante.Cognome = dichiaranteEsistente.Cognome;
                            aggiornare = true;
                        }

                        if (dichiarante.Sesso != dichiaranteEsistente.Sesso)
                        {
                            //dichiarante.Sesso = dichiaranteEsistente.Sesso;
                            aggiornare = true;
                        }

                        if (dichiarante.DataNascita != dichiaranteEsistente.DataNascita)
                        {
                            //dichiarante.DataNascita = dichiaranteEsistente.DataNascita;
                            aggiornare = true;
                        }

                        if (dichiarante.ComuneNascita != dichiaranteEsistente.ComuneNascita)
                        {
                            //dichiarante.ComuneNascita = dichiaranteEsistente.ComuneNascita;
                            aggiornare = true;
                        }

                        if (dichiarante.IndirizzoResidenza != dichiaranteEsistente.IndirizzoResidenza)
                        {
                            //dichiarante.IndirizzoResidenza = dichiaranteEsistente.IndirizzoResidenza;
                            aggiornare = true;
                        }

                        if (dichiarante.NumeroCivico != dichiaranteEsistente.NumeroCivico)
                        {
                            //dichiarante.NumeroCivico = dichiaranteEsistente.NumeroCivico;
                            aggiornare = true;
                        }

                        if (dichiarante.Parentela != dichiaranteEsistente.Parentela)
                        {
                            //dichiarante.Parentela = dichiaranteEsistente.Parentela;
                            aggiornare = true;
                        }

                        if (dichiarante.CodiceFamiglia != dichiaranteEsistente.CodiceFamiglia)
                        {
                            //dichiarante.CodiceFamiglia = dichiaranteEsistente.CodiceFamiglia;
                            aggiornare = true;
                        }
                    }
                }

                if (!esiste)
                {
                    // Aggiungo il nuovo Dichiarante alla lista
                    datiComplessivi.Dichiaranti.Add(dichiarante);
                }
                else if (aggiornare)
                {
                    // Aggiorno il Dichiarante esistente
                    datiComplessivi.DichiarantiDaAggiornare.Add(dichiarante);
                }


            }

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

            if (datiComplessivi.Dichiaranti.Count > 0)
            {
                logFile.LogInfo($"Trovati {datiComplessivi.Dichiaranti.Count} dichiaranti da aggiungere.");
            }
            else
            {
                logFile.LogInfo("Nessun dichiarante trovato da aggiungere.");
            }
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

    public static DatiCsvCompilati LeggiFilePhirana(string percorsoFile, int selectedEnteId, List<UtenzaIdrica> utenzeIdriche, ApplicationDbContext context)
    {
        var datiComplessivi = new DatiCsvCompilati();
        FileLog logFile = new FileLog($"wwwroot/log/Lettura_phirana.log");
        List<string> errori = new List<string>();
        List<string> warning = new List<string>();

        try
        {
            var righe = File.ReadAllLines(percorsoFile).Skip(1);

            int rigaCorrente = 1;
            logFile.LogInfo($"Nuovo caricamento dati phirana id Ente: {selectedEnteId}");
            logFile.LogInfo($"Numero di righe da elaborare: {righe.Count()} ");
            // Carico le toponimie esistenti dal database per l'ente specificato
            var toponimi = context.Toponomi.Where(s => s.IdEnte == selectedEnteId).ToList();

            // Inizializzazione sicura della lista dei toponimi
            if (toponimi == null)
            {
                logFile.LogInfo($"AVVISO: La lista di toponimi per l'ente {selectedEnteId} è null. Ne creo una nuova.");
                toponimi = new List<Toponimo>();
            }
            logFile.LogInfo($"Toponimi caricati per l'ente {selectedEnteId}: {toponimi.Count}");

            foreach (var riga in righe)
            {
                var error = false;
                rigaCorrente++;

                // 1. VERIFICO CHE I CAMPI SONO VALIDI

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
                // logFile.LogInfo($"CODICE Fiscale {FunzioniTrasversali.rimuoviVirgolette(campi[36])} | Riga {rigaCorrente} | lunghezza {FunzioniTrasversali.rimuoviVirgolette(campi[36]).Length} ");

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
                        // logFile.LogInfo("PIVA NULL && stato != 4, e sesso =D");
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

                // j) Verifico se il campo numero civico è presente

                if (string.IsNullOrWhiteSpace(FunzioniTrasversali.FormattaNumeroCivico(campi[16])))
                {
                    errori.Add($"Attenzione: Numero civico mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {FunzioniTrasversali.rimuoviVirgolette(campi[0])} | Matricola Contatore: {FunzioniTrasversali.rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                // Formatto l'indirizzo sono uguali
                var cod_fisc = FunzioniTrasversali.rimuoviVirgolette(campi[36]).ToUpper();
                var indirizzoUbicazione = FunzioniTrasversali.rimuoviVirgolette(campi[15]).ToUpper();
                var indirizzoRicavato = FunzioniTrasversali.FormattaIndirizzo(context, indirizzoUbicazione, cod_fisc, selectedEnteId);

                // if (indirizzoRicavato == null && cod_fisc != null)
                // {
                //     errori.Add($"Attenzione: Dichiarante e codice fiscale {cod_fisc} non trovato durante la ricerca sul idEnte {selectedEnteId}. Riga {rigaCorrente} | Nominativo {FunzioniTrasversali.rimuoviVirgolette(campi[32]).ToUpper()} {FunzioniTrasversali.rimuoviVirgolette(campi[33]).ToUpper()}");
                //     error = true;
                // }
                // else  if (indirizzoUbicazione != indirizzoRicavato)
                // {
                //     warning.Add($"Attenzione indirizzo mal formato per il dichiarante con codice fiscale {cod_fisc} si consiglia di aggiornarlo");
                // }

                if (string.IsNullOrEmpty(indirizzoRicavato))
                {
                    var findToponimo = new Toponimo
                    {
                        denominazione = indirizzoUbicazione,
                        IdEnte = selectedEnteId,
                    };

                    // Uso FirstOrDefault per trovare il toponimo esistente, che è l'approccio corretto.
                    var toponimoEsistente = toponimi.FirstOrDefault(t => t.denominazione.Equals(findToponimo.denominazione, StringComparison.OrdinalIgnoreCase) && t.IdEnte == findToponimo.IdEnte);

                    if (toponimoEsistente == null)
                    {
                        // Se non esiste, lo aggiungo.
                        findToponimo.normalizzazione = null;
                        findToponimo.data_creazione = DateTime.Now;
                        findToponimo.data_aggiornamento = null;

                        toponimi.Add(findToponimo);
                        indirizzoRicavato = indirizzoUbicazione;
                        datiComplessivi.Toponimi.Add(findToponimo); // Aggiungo alla lista per il DTO di ritorno
                        //logFile.LogInfo($"Riga {rigaCorrente}: Toponimo '{indirizzoUbicazione}' aggiunto.");
                    }
                    else
                    {
                        // Se esiste, lo utilizzo.
                        //logFile.LogInfo($"Riga {rigaCorrente}: Toponimo '{indirizzoUbicazione}' già esistente.");

                        if (toponimoEsistente.normalizzazione != null)
                        {
                            // Assegno l'indirizzo associato a quel toponimo
                            indirizzoRicavato = toponimoEsistente.normalizzazione;
                        }
                        else
                        {
                            indirizzoRicavato = indirizzoUbicazione;
                        }
                    }
                }
                else if (indirizzoRicavato != indirizzoUbicazione)
                {
                    // ... La tua logica per i warning o l'aggiornamento continua qui

                    warning.Add($"Attenzione indirizzo mal formato per il dichiarante con codice fiscale {cod_fisc} si consiglia di aggiornarlo");

                    var findToponimo = new Toponimo
                    {
                        denominazione = indirizzoUbicazione,
                        IdEnte = selectedEnteId,
                    };

                    // Uso FirstOrDefault per trovare il toponimo esistente, che è l'approccio corretto.
                    var toponimoEsistente = toponimi.FirstOrDefault(t => t.denominazione.Equals(findToponimo.denominazione, StringComparison.OrdinalIgnoreCase) && t.IdEnte == findToponimo.IdEnte);

                    if (toponimoEsistente == null)
                    {
                        // Se non esiste, lo aggiungo.
                        findToponimo.normalizzazione = indirizzoRicavato;
                        findToponimo.data_creazione = DateTime.Now;
                        findToponimo.data_aggiornamento = null;

                        toponimi.Add(findToponimo);
                        indirizzoRicavato = indirizzoUbicazione;
                        datiComplessivi.ToponimiDaAggiornare.Add(findToponimo); // Aggiungo alla lista per il DTO di ritorno
                        //logFile.LogInfo($"Riga {rigaCorrente}: Toponimo '{indirizzoUbicazione}' aggiunto.");
                    }
                    else
                    {
                        // Se esiste, lo utilizzo.
                        //logFile.LogInfo($"Riga {rigaCorrente}: Toponimo '{indirizzoUbicazione}' già esistente.");

                        if (toponimoEsistente.normalizzazione == null)
                        {
                            //logFile.LogInfo($"Aggiornamento toponimo {toponimoEsistente.ToString()}");
                            // Assegno l'indirizzo associato a quel toponimo
                            toponimoEsistente.normalizzazione = indirizzoRicavato;
                            toponimoEsistente.data_aggiornamento = DateTime.Now;
                        }
                    }
                }
                //logFile.LogInfo($"Indirizzo ricavato: {indirizzoRicavato}");
                // Controllo se sono presenti Errori
                //logFile.LogInfo($"ERROR: {error}");
                if (error)
                {
                    continue; // Salta la riga se ci sono errori
                }

                // 2) Verifico se l'utenza idrica è già presente
                var utenza = new UtenzaIdrica
                {
                    idAcquedotto = FunzioniTrasversali.rimuoviVirgolette(campi[0]),
                    stato = int.TryParse(FunzioniTrasversali.rimuoviVirgolette(campi[9]), out int stato) ? stato : 0,
                    periodoIniziale = FunzioniTrasversali.ConvertiData(FunzioniTrasversali.rimuoviVirgolette(campi[13])),
                    periodoFinale = FunzioniTrasversali.ConvertiData(FunzioniTrasversali.rimuoviVirgolette(campi[14])),
                    matricolaContatore = FunzioniTrasversali.rimuoviVirgolette(campi[12]).ToUpper(),
                    indirizzoUbicazione = indirizzoRicavato.ToUpper(),
                    numeroCivico = FunzioniTrasversali.FormattaNumeroCivico(campi[16]).ToUpper(),
                    subUbicazione = FunzioniTrasversali.rimuoviVirgolette(campi[17]).ToUpper(),
                    scalaUbicazione = FunzioniTrasversali.rimuoviVirgolette(campi[18]),
                    piano = FunzioniTrasversali.rimuoviVirgolette(campi[19]),
                    interno = FunzioniTrasversali.rimuoviVirgolette(campi[20]),
                    tipoUtenza = FunzioniTrasversali.rimuoviVirgolette(campi[26]).ToUpper(),
                    cognome = FunzioniTrasversali.rimuoviVirgolette(campi[32]).ToUpper(),
                    nome = FunzioniTrasversali.rimuoviVirgolette(campi[33]).ToUpper(),
                    sesso = FunzioniTrasversali.rimuoviVirgolette(campi[34]).ToUpper(),
                    codiceFiscale = cod_fisc,
                    IdEnte = selectedEnteId,
                };
                logFile.LogInfo($"Utenza: {utenza.ToString()}");
                // 2.g) Se l'utenza non esiste, la aggiungo alla lista delle utenze idriche
                datiComplessivi.UtenzeIdriche.Add(utenza);
            }

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

            // Faccio la stessa cosa per i warning
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

            if (datiComplessivi.UtenzeIdriche.Count > 0)
            {
                logFile.LogInfo($"Numero di utenze idriche aggiunte: {datiComplessivi.UtenzeIdriche.Count}");
            }
            else
            {
                logFile.LogInfo("Nessuna utenza nuova idrica trovata.");
            }

            if (datiComplessivi.UtenzeIdricheEsistente.Count > 0)
            {
                logFile.LogInfo($"Numero di utenze idriche esistenti aggiornate: {datiComplessivi.UtenzeIdricheEsistente.Count}");
            }
            else
            {
                logFile.LogInfo("Nessuna utenza idrica esistente trovata da aggiornare.");
            }

            if (datiComplessivi.Toponimi.Count > 0)
            {
                logFile.LogInfo($"Numero di Toponimi Aggiunti: {datiComplessivi.Toponimi.Count}");
            }
            else
            {
                logFile.LogInfo("Nessun toponimo aggiunto");
            }

            if (datiComplessivi.ToponimiDaAggiornare.Count > 0)
            {
                logFile.LogInfo($"Toponimi Aggiornati: {datiComplessivi.ToponimiDaAggiornare.Count}");
            }else{
                logFile.LogInfo($"Nessun toponimo aggiornato!");
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


    public static DatiCsvCompilati LeggiFileINPS(string percorsoFile, ApplicationDbContext context, int selectedEnteId)
    {
        var datiComplessivi = new DatiCsvCompilati();
        FileLog logFile = new FileLog($"Elaborazione_INPS.log");
        List<string> errori = new List<string>();
        try
        {
            var righe = File.ReadAllLines(percorsoFile).Skip(1);

            int rigaCorrente = 1;
            logFile.LogInfo($"Numero di righe da elaborare: {righe.Count()}");
            var dichiaranti = context.Dichiaranti.ToList();

            foreach (var riga in righe)
            {
                var error = false;
                rigaCorrente++;

                // 1. Verifico se i campi del file sono corretti e validi per effettuare le operazioni successive

                // verifico se la riga è vuota
                if (string.IsNullOrWhiteSpace(riga)) continue;

                var campi = riga.Split(CsvDelimiter);

                // Verifico se la riga ha almeno 15 campi
                if (campi.Length < 15)
                {
                    errori.Add($"Riga {rigaCorrente} malformata, saltata. Numero di campi: {campi.Length}. Attesi almeno 15.");
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

                // Verifico se il campo numero_civico è presente e non vuotO
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

                if (error)
                {
                    continue; // Salta la riga se ci sono errori
                }


                // 1.b) Mi salvo i campi presi dal file CSV in modo da poter effettuare le operazioni successive

                string idAto = FunzioniTrasversali.rimuoviVirgolette(campi[0]);
                string codiceBonus = FunzioniTrasversali.rimuoviVirgolette(campi[1]).ToUpper();
                string codiceFiscale = FunzioniTrasversali.rimuoviVirgolette(campi[2]).ToUpper();

                string nomeDichiarante = FunzioniTrasversali.rimuoviVirgolette(campi[3]).ToUpper();
                string cognomeDichiarante = FunzioniTrasversali.rimuoviVirgolette(campi[4]).ToUpper();
                string[] codiciFiscaliFamigliari = FunzioniTrasversali.splitCodiceFiscale(campi[5]);

                string annoValidita = FunzioniTrasversali.rimuoviVirgolette(campi[6]);
                string dataInizioValidita = FunzioniTrasversali.rimuoviVirgolette(campi[7]);
                string dataFineValidita = FunzioniTrasversali.rimuoviVirgolette(campi[8]);

                string indirizzoAbitazione = FunzioniTrasversali.rimuoviVirgolette(campi[9]).ToUpper();
                string numeroCivico = FunzioniTrasversali.FormattaNumeroCivico(campi[10]).ToUpper();
                string istatAbitazione = FunzioniTrasversali.rimuoviVirgolette(campi[11]).ToUpper();
                string capAbitazione = FunzioniTrasversali.rimuoviVirgolette(campi[12]).ToUpper();
                string provinciaAbitazione = FunzioniTrasversali.rimuoviVirgolette(campi[13]).ToUpper();

                string presenzaPod = FunzioniTrasversali.rimuoviVirgolette(campi[14]).ToUpper();
                string numeroComponenti = FunzioniTrasversali.rimuoviVirgolette(campi[15]).ToUpper();
                DateTime dataCreazione = DateTime.Now;

                // 1.c) Aggiungo dei campi aggiuntivi neccessari per la creazione del report
                string esitoStr = "No";
                string esito = "04"; // 01 = fornitura diretta, 02 = fornitura indiretta, 03 = fornitura diretta non rispetta requisiti, 04 =  fornitura indiretta non rispetta requisiti 
                int? idFornituraIdrica = null;

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

                // 2.b) Verifico se i campi ISTAT, CAP e provincia corrispondono a l'ente selezionato il quale gestisce le utenze idriche

                if (!(istatEnte != istatAbitazione || capEnte != capAbitazione || provinciaAbitazione != ente.Provincia))
                {
                    // 2.c) se i campi corispondono verifico se il richiedente è residente nel comune selezionato
                    var dichiarantiFiltratiPerNomeEnte = dichiaranti.Where(s => s.CodiceFiscale == codiceFiscale && s.IdEnte == selectedEnteId).ToList();
                    if (dichiarantiFiltratiPerNomeEnte.Count == 1)
                    {
                        // 2.c.1) se è residente nel comune selzionato allora esito è uguale a Si
                        esitoStr = "Si";

                        //3.a) verifica se il richiedente ha una fornitura idrica diretta 

                        (string esitoRestituito, int? idFornituraTrovato) = FunzioniTrasversali.verificaEsisistenzaFornitura(codiceFiscale, selectedEnteId, context, dichiarantiFiltratiPerNomeEnte[0].IndirizzoResidenza, dichiarantiFiltratiPerNomeEnte[0].NumeroCivico);
                        idFornituraIdrica = idFornituraTrovato;
                        if (esitoRestituito == "01")
                        {
                            esito = "01";
                        }
                        else if (esitoRestituito == "03")
                        {
                            esito = "03";
                        }
                        else if (esitoRestituito == "04")
                        {
                            if (codiciFiscaliFamigliari.Length > 0)
                            {
                                foreach (var codFisc in codiciFiscaliFamigliari)
                                {
                                    var dichiaranteFamigliare = dichiaranti.Where(s => s.CodiceFiscale == codFisc && s.IdEnte == selectedEnteId).ToList();
                                    if (dichiaranteFamigliare.Count == 1)
                                    {
                                        // Verifico se il membro della famiglia ha una fornitura idrica diretta
                                        (string esitoFamigliare, int? idFornituraMembro) = FunzioniTrasversali.verificaEsisistenzaFornitura(codFisc, selectedEnteId, context, dichiarantiFiltratiPerNomeEnte[0].IndirizzoResidenza, dichiarantiFiltratiPerNomeEnte[0].NumeroCivico);
                                        idFornituraIdrica = idFornituraMembro;
                                        if (esitoFamigliare == "01")
                                        {
                                            esito = "01"; // Se uno dei membri della famiglia ha una fornitura diretta, l'esito è 01
                                            break;
                                        }
                                        else if (esitoFamigliare == "03")
                                        {
                                            esito = "03";
                                            break;
                                        }
                                        else if (esitoFamigliare == "04")
                                        {
                                            // 3.g) Verifico se Presenza_POD è SI
                                            if (presenzaPod.Equals("Si", StringComparison.OrdinalIgnoreCase))
                                            {
                                                esito = "02"; // Se nessun membro della famiglia ha una fornitura diretta, ma Presenza_POD è SI, l'esito è 02
                                            }
                                        }
                                    }
                                }

                            }
                        }

                    }
                }
                else
                {
                    // Implementare logica per gestire il caso in cui i campi non corrispondono
                    continue; // Salta la riga se i campi non corrispondono
                }

                // 4) Creo un nuovo report con i dati raccolti
                var report = new Report
                {
                    idAto = idAto,
                    codiceBonus = codiceBonus,
                    idFornitura = idFornituraIdrica,
                    codiceFiscale = codiceFiscale,
                    nomeDichiarante = nomeDichiarante,
                    cognomeDichiarante = cognomeDichiarante,
                    annoValidita = annoValidita,
                    dataInizioValidita = FunzioniTrasversali.ConvertiData(dataInizioValidita),
                    dataFineValidita = FunzioniTrasversali.ConvertiData(dataFineValidita),
                    indirizzoAbitazione = indirizzoAbitazione,
                    numeroCivico = numeroCivico,
                    istat = istatAbitazione,
                    capAbitazione = capAbitazione,
                    provinciaAbitazione = provinciaAbitazione,
                    presenzaPod = presenzaPod,
                    numeroComponenti = int.Parse(numeroComponenti),
                    esitoStr = esitoStr,
                    esito = esito,
                    IdEnte = selectedEnteId,
                    DataCreazione = dataCreazione
                };

                datiComplessivi.reports.Add(report);

                // salvo il report nel contesto del database
                //logFile.LogInfo($"Riga {rigaCorrente}: Report creato: idAto: {report.idAto}, CodiceBonus: {report.codiceBonus}, CodiceFiscale: {report.codiceFiscale}, NomeDichiarante: {report.nomeDichiarante}, CognomeDichiarante: {report.cognomeDichiarante}, Esito: {report.esitoStr}, Esito numerico: {report.esito}, DataInizioValidita: {report.dataInizioValidita}, DataFineValidita: {report.dataFineValidita}");
            }
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

}