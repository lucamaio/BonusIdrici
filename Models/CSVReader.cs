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

                if (string.IsNullOrEmpty(rimuoviVirgolette(campi[0])))
                {
                    errori.Add($"Attenzione: Cognome mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // d) Verifico che il campo nome è valido

                if (string.IsNullOrEmpty(rimuoviVirgolette(campi[1])))
                {
                    errori.Add($"Attenzione: Nome mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // e) Verifico che il Codice Fiscale è valido

                if (string.IsNullOrEmpty(rimuoviVirgolette(campi[2])) || rimuoviVirgolette(campi[2]).Length != 16)
                {
                    errori.Add($"Attenzione: Codice Fiscale mancante o mal formato, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // f) Verifico il campo sesso se è valido

                if (string.IsNullOrEmpty(rimuoviVirgolette(campi[3])) && (rimuoviVirgolette(campi[3]).ToUpper() != "M") && (rimuoviVirgolette(campi[3]).ToUpper() != "F"))
                {
                    errori.Add($"Attenzione: Sesso mancante o mal formato, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // g) Verifico se il campo indirizzo residente è presente

                if (string.IsNullOrEmpty(rimuoviVirgolette(campi[7])))
                {
                    errori.Add($"Attenzione: Indirizzo residenza mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // h) Verifico se il campo numero civico è presente

                if (string.IsNullOrWhiteSpace(FormattaNumeroCivico(campi[8])))
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
                    Cognome = rimuoviVirgolette(campi[0]).ToUpper(),
                    Nome = rimuoviVirgolette(campi[1]).ToUpper(),
                    CodiceFiscale = rimuoviVirgolette(campi[2]).ToUpper(),
                    Sesso = rimuoviVirgolette(campi[3]).ToUpper(),
                    DataNascita = ConvertiData(campi[4]),
                    ComuneNascita = rimuoviVirgolette(campi[5]).ToUpper(),
                    IndirizzoResidenza = rimuoviVirgolette(campi[7]).ToUpper(),
                    NumeroCivico = FormattaNumeroCivico(campi[8]),
                    Parentela = rimuoviVirgolette(campi[10]).ToUpper(),
                    CodiceFamiglia = int.Parse(campi[11].Trim()),
                    NumeroComponenti = int.Parse(campi[13].Trim()),
                    CodiceFiscaleIntestatarioScheda = rimuoviVirgolette(campi[19]),
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

                if (string.IsNullOrEmpty(rimuoviVirgolette(campi[0])))
                {
                    errori.Add($"Attenzione: Id Acquedotto mancante, saltata. | Riga {rigaCorrente} | Nominativo: {rimuoviVirgolette(campi[32])} {rimuoviVirgolette(campi[33])} | Codice Fiscale: {rimuoviVirgolette(campi[36])}");
                    error = true;
                }

                // d) Controllo se il campo Codice Fiscale è valido è != null
                // logFile.LogInfo($"CODICE Fiscale {rimuoviVirgolette(campi[36])} | Riga {rigaCorrente} | lunghezza {rimuoviVirgolette(campi[36]).Length} ");

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[36])))
                {
                    // logFile.LogInfo($"SONO NULL! | SESSO: {rimuoviVirgolette(campi[34])}");
                    if (!rimuoviVirgolette(campi[34]).Equals("D", StringComparison.OrdinalIgnoreCase))
                    {
                        logFile.LogInfo("COD FISC MANCATE E sesso != D");
                        errori.Add($"Attenzione : Codice Fiscale mancante, saltata. | Riga {rigaCorrente} | Nominativo: {rimuoviVirgolette(campi[32])} {rimuoviVirgolette(campi[33])}");
                        error = true;
                    }
                    else if (string.IsNullOrEmpty(rimuoviVirgolette(campi[37])) &&
                            rimuoviVirgolette(campi[9]) != "4" &&
                            rimuoviVirgolette(campi[9]) != "5" &&
                            string.IsNullOrEmpty(rimuoviVirgolette(campi[14])) &&
                            rimuoviVirgolette(campi[34]).Equals("D", StringComparison.OrdinalIgnoreCase))
                    {
                        // logFile.LogInfo("PIVA NULL && stato != 4, e sesso =D");
                        warning.Add($"Attenzione: Codice Fiscale e Partita IVA della ditta {rimuoviVirgolette(campi[32])} non presente. (Questo non è un errore, ma una segnalazione). | Riga {rigaCorrente}");
                        error = true;
                    }
                }
                else if (rimuoviVirgolette(campi[36]).Length != 16)
                {
                    // logFile.LogInfo($"mal formata! | SESSO: {rimuoviVirgolette(campi[34])}");
                    if (!rimuoviVirgolette(campi[34]).Equals("D", StringComparison.OrdinalIgnoreCase))
                    {
                        // logFile.LogInfo("Sesso diverso da D!");
                        errori.Add($"Attenzione : Codice Fiscale mal formato, saltata. | Riga {rigaCorrente} | Nominativo: {rimuoviVirgolette(campi[32])} {rimuoviVirgolette(campi[33])}");
                        error = true;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(rimuoviVirgolette(campi[37])) &&
                            rimuoviVirgolette(campi[9]) != "4" &&
                            rimuoviVirgolette(campi[9]) != "5" &&
                            string.IsNullOrEmpty(rimuoviVirgolette(campi[14])) &&
                            rimuoviVirgolette(campi[34]).Equals("D", StringComparison.OrdinalIgnoreCase))
                        {
                            // logFile.LogInfo("PIVA 2 NULL && stato != 4, e sesso =D");
                            warning.Add($"Attenzione: Codice Fiscale e Partita IVA della ditta {rimuoviVirgolette(campi[32])} non trovati. (Questo non è un errore, ma una segnalazione). | Riga {rigaCorrente}");
                            error = true;
                        }
                    }
                }


                // e) Controllo se la matricola del contatore è presente

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[12])) && rimuoviVirgolette(campi[9]) != "4" && rimuoviVirgolette(campi[9]) != "5" && string.IsNullOrEmpty(rimuoviVirgolette(campi[14])))
                {

                    errori.Add($"Attenzione: Matricola Contatore mancante, saltata. Riga {rigaCorrente} | Nominativo: {rimuoviVirgolette(campi[32])} {rimuoviVirgolette(campi[33])} | Codice Fiscale: {rimuoviVirgolette(campi[36])}");
                    error = true;
                }

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[0])))
                {
                    errori.Add($"Attenzione: Id Acquedotto mancante, saltata. Riga {rigaCorrente}  | Nominativo: {rimuoviVirgolette(campi[32])} {rimuoviVirgolette(campi[33])} | Codice Fiscale: {rimuoviVirgolette(campi[36])}");
                    error = true;
                }

                // f) Controllo se i campi nomi, cognome e sesso sono presenti

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[32])) && string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[33])) && rimuoviVirgolette(campi[34]).ToUpper() != "D")
                {
                    errori.Add($"Attenzione: Nome o Cognome mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {rimuoviVirgolette(campi[0])} | Matricola Contatore: {rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[34])) ||
                    (rimuoviVirgolette(campi[34]).ToUpper() != "M" && rimuoviVirgolette(campi[34]).ToUpper() != "F" && rimuoviVirgolette(campi[34]).ToUpper() != "D"))
                {
                    errori.Add($"Attenzione: Sesso mancante o mal formato, saltata. | Riga {rigaCorrente} | idAcquedotto : {rimuoviVirgolette(campi[0])} | Matricola Contatore: {rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                // g) Controllo se il campo periodoIniziale è presente

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[13])))
                {
                    errori.Add($"Attenzione: Periodo iniziale mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {rimuoviVirgolette(campi[0])} | Matricola Contatore: {rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                // h) Verifico se il campo tipo utenza è presente

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[26])))
                {
                    errori.Add($"Attenzione: Tipo Utenza mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {rimuoviVirgolette(campi[0])} | Matricola Contatore: {rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                // i) Verifico se il campo indirizzo Ubicazione è presente

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[15])))
                {
                    errori.Add($"Attenzione: Indirizzo ubicazione mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {rimuoviVirgolette(campi[0])} | Matricola Contatore: {rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                // j) Verifico se il campo numero civico è presente

                if (string.IsNullOrWhiteSpace(FormattaNumeroCivico(campi[16])))
                {
                    errori.Add($"Attenzione: Numero civico mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {rimuoviVirgolette(campi[0])} | Matricola Contatore: {rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                // Formatto l'indirizzo sono uguali
                var cod_fisc = rimuoviVirgolette(campi[36]).ToUpper();
                var indirizzoUbicazione = rimuoviVirgolette(campi[15]).ToUpper();
                var indirizzoRicavato = FormattaIndirizzo(context, indirizzoUbicazione, cod_fisc, selectedEnteId);

                // if (indirizzoRicavato == null && cod_fisc != null)
                // {
                //     errori.Add($"Attenzione: Dichiarante e codice fiscale {cod_fisc} non trovato durante la ricerca sul idEnte {selectedEnteId}. Riga {rigaCorrente} | Nominativo {rimuoviVirgolette(campi[32]).ToUpper()} {rimuoviVirgolette(campi[33]).ToUpper()}");
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
                        findToponimo.nomarlizzazione = null;
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

                        if (toponimoEsistente.nomarlizzazione != null)
                        {
                            // Assegno l'indirizzo associato a quel toponimo
                            indirizzoRicavato = toponimoEsistente.nomarlizzazione;
                        }
                        else
                        {
                            indirizzoRicavato = indirizzoUbicazione;
                        }
                    }
                }
<<<<<<< HEAD
                else if (indirizzoRicavato != indirizzoUbicazione)
=======
                // else if (cod_fisc == null)
                // {
                //     warning.Add($"Attenzione: CODICE FISCALE MANCANTE. Riga {riga} | idAcquedotto : {rimuoviVirgolette(campi[0])} | Matricola Contatore: {rimuoviVirgolette(campi[12])}");
                // }
                else  if (indirizzoUbicazione != indirizzoRicavato)
>>>>>>> f843c930ff350d45a33002e0b4c759dc1af88df3
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
                        findToponimo.nomarlizzazione = indirizzoRicavato;
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

                        if (toponimoEsistente.nomarlizzazione == null)
                        {
                            //logFile.LogInfo($"Aggiornamento toponimo {toponimoEsistente.ToString()}");
                            // Assegno l'indirizzo associato a quel toponimo
                            toponimoEsistente.nomarlizzazione = indirizzoRicavato;
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
                    idAcquedotto = rimuoviVirgolette(campi[0]),
                    stato = int.TryParse(rimuoviVirgolette(campi[9]), out int stato) ? stato : 0,
                    periodoIniziale = ConvertiData(rimuoviVirgolette(campi[13])),
                    periodoFinale = ConvertiData(rimuoviVirgolette(campi[14])),
                    matricolaContatore = rimuoviVirgolette(campi[12]).ToUpper(),
                    indirizzoUbicazione = indirizzoRicavato.ToUpper(),
                    numeroCivico = FormattaNumeroCivico(campi[16]).ToUpper(),
                    subUbicazione = rimuoviVirgolette(campi[17]).ToUpper(),
                    scalaUbicazione = rimuoviVirgolette(campi[18]),
                    piano = rimuoviVirgolette(campi[19]),
                    interno = rimuoviVirgolette(campi[20]),
                    tipoUtenza = rimuoviVirgolette(campi[26]).ToUpper(),
                    cognome = rimuoviVirgolette(campi[32]).ToUpper(),
                    nome = rimuoviVirgolette(campi[33]).ToUpper(),
                    sesso = rimuoviVirgolette(campi[34]).ToUpper(),
                    codiceFiscale = cod_fisc,
                    IdEnte = selectedEnteId,
                };
<<<<<<< HEAD
                logFile.LogInfo($"Utenza: {utenza.ToString()}");
                // 2.g) Se l'utenza non esiste, la aggiungo alla lista delle utenze idriche
                datiComplessivi.UtenzeIdriche.Add(utenza);
=======

                var esiste = false;
                // var aggiornare = false;

                // if (utenzeIdriche != null && utenza != null)
                // {
                //     // 2.a) Verifico se l'utenza idrica è già presente
                //     var utenzaEsistente = utenzeIdriche.Find(u => u.idAcquedotto == utenza.idAcquedotto && u.codiceFiscale == utenza.codiceFiscale);

                //     Console.WriteLine($"Verifico se l'utenza idrica esiste: {utenza.idAcquedotto} - {utenza.codiceFiscale}");
                //     if (utenzaEsistente != null)
                //     {
                //         Console.WriteLine($"Utenza idrica esistente trovata:{utenzaEsistente.ToString()}");
                //         esiste = true;
                //         // Utenza già presente, controllo se devo aggiornare i campi
                //         // 2.b) Verifico lo stato dell'utenza
                //         if (utenza.stato != utenzaEsistente.stato)
                //         {
                //             utenza.stato = utenzaEsistente.stato;
                //             aggiornare = true;
                //         }

                //         //2.c) Verifico se il periodo Finale è diverso
                //         // if (utenza.periodoFinale != utenzaEsistente.periodoFinale)
                //         // {
                //         //     utenza.periodoFinale = utenzaEsistente.periodoFinale; 
                //         //     aggiornare = true;
                //         // }

                //         // 2.d) Verifico se la matricola del contatore è diversa
                //         if (utenza.matricolaContatore != utenzaEsistente.matricolaContatore)
                //         {
                //             utenza.matricolaContatore = utenzaEsistente.matricolaContatore;
                //             aggiornare = true;
                //         }

                //         // 2.e) Verifico se l'indirizzo ubicazione è diverso
                //         if (utenza.indirizzoUbicazione != utenzaEsistente.indirizzoUbicazione)
                //         {
                //             utenza.indirizzoUbicazione = utenzaEsistente.indirizzoUbicazione;
                //             aggiornare = true;
                //         }

                //         if (utenza.numeroCivico != utenzaEsistente.numeroCivico)
                //         {
                //             utenza.numeroCivico = utenzaEsistente.numeroCivico;
                //             aggiornare = true;
                //         }

                //         if (!string.IsNullOrEmpty(utenza.subUbicazione) && utenza.subUbicazione != utenzaEsistente.subUbicazione)
                //         {
                //             utenza.subUbicazione = utenzaEsistente.subUbicazione;
                //             aggiornare = true;
                //         }

                //         if (!string.IsNullOrEmpty(utenza.scalaUbicazione) && utenza.scalaUbicazione != utenzaEsistente.scalaUbicazione)
                //         {
                //             utenza.scalaUbicazione = utenzaEsistente.scalaUbicazione;
                //             aggiornare = true;
                //         }

                //         if (!string.IsNullOrEmpty(utenza.piano) && utenza.piano != utenzaEsistente.piano)
                //         {
                //             utenza.piano = utenzaEsistente.piano;
                //             aggiornare = true;
                //         }

                //         if (!string.IsNullOrEmpty(utenza.interno) && utenza.interno != utenzaEsistente.interno)
                //         {
                //             utenza.interno = utenzaEsistente.interno;
                //             aggiornare = true;
                //         }

                //         // 2.f) Verifico se il tipo utenza è diverso
                //         if (utenza.tipoUtenza != utenzaEsistente.tipoUtenza)
                //         {
                //             utenza.tipoUtenza = utenzaEsistente.tipoUtenza;
                //             aggiornare = true;
                //         }
                //     }
                // }

                if (!esiste)
                {
                    // 2.g) Se l'utenza non esiste, la aggiungo alla lista delle utenze idriche
                    datiComplessivi.UtenzeIdriche.Add(utenza);
                }
                // else if (aggiornare)
                // {
                //     datiComplessivi.UtenzeIdricheEsistente.Add(utenza);
                // }
>>>>>>> f843c930ff350d45a33002e0b4c759dc1af88df3
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
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[0])))
                {
                    errori.Add($"ID_ATO mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo codice_bonus è presente, non vuoto è ha una lunhezza di 15 carratteri

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[1])) || rimuoviVirgolette(campi[1]).Length != 15)
                {
                    errori.Add($"Attenzione: Codice Bonus mancante o non valido, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo Codice Fiscale è presente e non vuoto

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[2])))
                {
                    errori.Add($"Attenzione: Codice Fiscale mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo NOME_DICHIARANTE e COGNOME_DICHIARANTE sono presenti e non vuoti
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[3])) || string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[4])))
                {
                    errori.Add($"Attenzione: Nome o Cognome mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo Anno_validità è presente e non vuoto

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[6])))
                {
                    errori.Add($"Attenzione: Anno di validità mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // verifico se il campo Data_inizio_validità è presente e non vuoto
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[7])))
                {
                    errori.Add($"Attenzione: Data di inizio validità mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo Data_fine_validità è presente e non vuoto
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[8])))
                {
                    errori.Add($"Attenzione: Data di fine validità mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo indirizzo_abitazione è presente e non vuoto
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[9])))
                {
                    errori.Add($"Attenzione: Indirizzo abitazione mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo numero_civico è presente e non vuotO
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[10])))
                {
                    errori.Add($"Attenzione: Numero civico mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo iSTAT è presente e non vuoto
                string istat = rimuoviVirgolette(campi[11]);

                if (string.IsNullOrWhiteSpace(istat) || istat.Length != 6)
                {
                    errori.Add($"Attenzione: ISTAT mancante o malformata, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // VERIFICO Se il campo cap è presente e non vuoto
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[12])) || rimuoviVirgolette(campi[12]).Length != 5)
                {
                    errori.Add($"Attenzione: CAP mancante o malformato, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // vrifico se il campo provincia_abitazione è presente e non vuoto
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[13])) || rimuoviVirgolette(campi[13]).Length != 2)
                {
                    errori.Add($"Attenzione: Provincia abitazione mancante o malformata, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // Verifico se il campo Presenza_POD è presente ed è valido
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[14])) ||
                    (!rimuoviVirgolette(campi[14]).Equals("SI", StringComparison.OrdinalIgnoreCase) &&
                     !rimuoviVirgolette(campi[14]).Equals("NO", StringComparison.OrdinalIgnoreCase)))
                {
                    errori.Add($"Attenzione: Presenza POD mancante o non valida, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                // verifdico se il campo n_componenti è presente e non vuoto
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[15])))
                {
                    errori.Add($"Attenzione: Numero componenti mancante, saltata. Riga {rigaCorrente}");
                    error = true;
                }

                if (error)
                {
                    continue; // Salta la riga se ci sono errori
                }


                // 1.b) Mi salvo i campi presi dal file CSV in modo da poter effettuare le operazioni successive

                string idAto = rimuoviVirgolette(campi[0]);
                string codiceBonus = rimuoviVirgolette(campi[1]).ToUpper();
                string codiceFiscale = rimuoviVirgolette(campi[2]).ToUpper();

                string nomeDichiarante = rimuoviVirgolette(campi[3]).ToUpper();
                string cognomeDichiarante = rimuoviVirgolette(campi[4]).ToUpper();
                string[] codiciFiscaliFamigliari = splitCodiceFiscale(campi[5]);

                string annoValidita = rimuoviVirgolette(campi[6]);
                string dataInizioValidita = rimuoviVirgolette(campi[7]);
                string dataFineValidita = rimuoviVirgolette(campi[8]);

                string indirizzoAbitazione = rimuoviVirgolette(campi[9]).ToUpper();
                string numeroCivico = FormattaNumeroCivico(campi[10]).ToUpper();
                string istatAbitazione = rimuoviVirgolette(campi[11]).ToUpper();
                string capAbitazione = rimuoviVirgolette(campi[12]).ToUpper();
                string provinciaAbitazione = rimuoviVirgolette(campi[13]).ToUpper();

                string presenzaPod = rimuoviVirgolette(campi[14]).ToUpper();
                string numeroComponenti = rimuoviVirgolette(campi[15]).ToUpper();
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

                        (string esitoRestituito, int? idFornituraTrovato) = verificaEsisistenzaFornitura(codiceFiscale, selectedEnteId, context, dichiarantiFiltratiPerNomeEnte[0].IndirizzoResidenza, dichiarantiFiltratiPerNomeEnte[0].NumeroCivico);
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
                                        (string esitoFamigliare, int? idFornituraMembro) = verificaEsisistenzaFornitura(codFisc, selectedEnteId, context, dichiarantiFiltratiPerNomeEnte[0].IndirizzoResidenza, dichiarantiFiltratiPerNomeEnte[0].NumeroCivico);
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
                    dataInizioValidita = ConvertiData(dataInizioValidita),
                    dataFineValidita = ConvertiData(dataFineValidita),
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

    private static string rimuoviVirgolette(string stringa)
    {
        string formatedString = stringa.Trim();
        if (formatedString.Contains("\""))
        {
            // Rimuove gli spazi e i caratteri speciali come / e -
            formatedString = formatedString.Replace("\"", "");
        }
        return formatedString;
    }



    private static string FormattaNumeroCivico(string stringa)
    {
        // 1. Pulizia e normalizzazione iniziale
        // Rimuove virgolette, trimma spazi e converte in maiuscolo
        string numero_civico = rimuoviVirgolette(stringa).ToUpperInvariant();

        // 2. Gestione casi speciali: null/vuoto, "0", "SN"
        if (string.IsNullOrEmpty(numero_civico))
        {
            return "N/A"; // O string.Empty, a seconda della tua preferenza per numeri civici mancanti
        }

        if (numero_civico.Equals("0") || numero_civico.Equals("SN"))
        {
            return "SNC"; // "Senza Numero Civico"
        }

        // 3. Verifica la presenza di separatori ('/', '-') per estrarre la parte iniziale
        // Se vuoi considerare anche lo spazio come separatore, aggiungilo qui:
        // int indiceSpazio = numero_civico.IndexOf(' ');

        int indiceSeparatore = numero_civico.IndexOf('/');
        int indiceDash = numero_civico.IndexOf('-');

        // Inizializza firstDelimiterIndex a un valore che indica che nessun delimitatore è stato trovato
        // (es. lunghezza della stringa, così Substring non fallirà se non ci sono delimitatori)
        int firstDelimiterIndex = numero_civico.Length;

        if (indiceSeparatore != -1)
        {
            firstDelimiterIndex = Math.Min(firstDelimiterIndex, indiceSeparatore);
        }
        if (indiceDash != -1)
        {
            firstDelimiterIndex = Math.Min(firstDelimiterIndex, indiceDash);
        }

        // Se un delimitatore è stato trovato (firstDelimiterIndex è minore della lunghezza originale)
        if (firstDelimiterIndex < numero_civico.Length)
        {
            // Estrai la sottostringa fino al primo delimitatore
            string parteIniziale = numero_civico.Substring(0, firstDelimiterIndex);

            // Applica Regex.Replace solo alla parte iniziale per rimuovere eventuali caratteri non numerici
            // Questo gestirà casi come "10A/B" -> "10A" -> "10"
            return Regex.Replace(parteIniziale, @"[^\d]", "");
        }
        else
        {
            // Nessun delimitatore '/' o '-' trovato.
            // Applica Regex.Replace all'intera stringa per rimuovere caratteri non numerici.
            // Questo gestirà casi come "10C" -> "10" o "VIA12" -> "12"
            return Regex.Replace(numero_civico, @"[^\d]", "");
        }
    }

    private static string[] splitCodiceFiscale(string codiceFiscale)
    {
        // Rimuove gli spazi e i caratteri speciali come / e -
        string formatedCodiceFiscale = codiceFiscale.Trim().Replace("\"", "");
        return formatedCodiceFiscale.Split(',');
    }


    public static DateTime? ConvertiData(string dataStringa)
    {
        if (string.IsNullOrWhiteSpace(dataStringa))
        {
            return null; // Restituisce null se la stringa è vuota/nulla
        }

        // Formato comune italiano: "gg/MM/aaaa"
        if (DateTime.TryParseExact(dataStringa, "dd/MM/yyyy", new CultureInfo("it-IT"), DateTimeStyles.None, out DateTime parsedDate))
        {
            return parsedDate;
        }

        // Se hai altri formati possibili nel CSV (es. "aaaa-MM-gg"), aggiungili qui:
        if (DateTime.TryParseExact(dataStringa, "yyyy-MM-dd", new CultureInfo("it-IT"), DateTimeStyles.None, out parsedDate))
        {
            return parsedDate;
        }

        // Se nessun formato corrisponde, stampa un avviso e restituisci null
        Console.WriteLine($"Attenzione: Impossibile convertire la data '{dataStringa}' nel formato atteso. Verrà salvato un valore nullo.");
        return null;
    }


    private static (string esito, int? idFornitura) verificaEsisistenzaFornitura(string codiceFiscale, int selectedEnteId, ApplicationDbContext context, string IndirizzoResidenza, string NumeroCivico)
    {
        // Verifica se il richiedente ha una fornitura idrica diretta
        var forniture = context.UtenzeIdriche.Where(s => s.codiceFiscale == codiceFiscale && s.IdEnte == selectedEnteId).ToList();
        if (forniture.Count == 1)
        {
            string? tipoUtenza = forniture[0].tipoUtenza;
            int? idFornituraTrovata = int.Parse(forniture[0].idAcquedotto);       //  Aggiungo una variabile per salvare id della fornitura

            if (tipoUtenza.Equals("UTENZA DOMESTICA", StringComparison.OrdinalIgnoreCase))
            {
                // 3.c) adessso verifico se l'utenza è situata nello stesso indirizzo del richiedente
                string? indirizzoUtenza = forniture[0].indirizzoUbicazione;
                string? numeroCivicoUtenza = forniture[0].numeroCivico;    // N.B Crea una funzione per formattare il numero civico come in FormattaNumeroCivico

                if (indirizzoUtenza.Equals(IndirizzoResidenza, StringComparison.OrdinalIgnoreCase) &&
                    numeroCivicoUtenza.Equals(NumeroCivico, StringComparison.OrdinalIgnoreCase))
                {
                    // 3.d) Adesso verifico se lo stato della fornitura e compresso tra 1 e 3
                    if (forniture[0].stato >= 1 && forniture[0].stato <= 3)
                    {
                        // 3.e) se lo stato è compreso tra 1 e 3 allora esito è uguale a 01
                        return ("01", idFornituraTrovata);
                    }
                    else
                    {
                        // 3.e.2) se lo stato non è compreso tra 1 e 3 allora esito è uguale a 03
                        return ("03", idFornituraTrovata);
                    }
                }
                else
                {
                    // 3.c.2) se l'utenza non è situata nello stesso indirizzo allora esito è uguale a 03
                    return ("03", idFornituraTrovata);
                }
            }
        }
        return ("04", null); // Nessuna fornitura
    }

   private static string? FormattaIndirizzo(ApplicationDbContext context, string indirizzo_ubicazione, string codiceFiscale, int IdEnte)
{
    // 1. Recupero il dichiarante
    var dichiarante = context.Dichiaranti.FirstOrDefault(s => s.CodiceFiscale == codiceFiscale && s.IdEnte==IdEnte);
    if (dichiarante == null) { return null; }

    var indirizzoResidenza = rimuoviVirgolette(dichiarante.IndirizzoResidenza).Trim();
    var indirizzoUbicazione = rimuoviVirgolette(indirizzo_ubicazione).Trim();

    // 2. Confronto diretto ignorando le maiuscole
    if (string.Equals(indirizzoUbicazione, indirizzoResidenza, StringComparison.OrdinalIgnoreCase))
    {
        return indirizzoUbicazione;
    }

    // 3. Suddivido la via di ubicazione in token significativi
    var partiIndirizzoUb = indirizzoUbicazione.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
    var paroleDaIgnorare = new HashSet<string> { "via", "viale", "piazza", "corso", "strada" };

    foreach (var part in partiIndirizzoUb)
    {
        var token = part.ToLowerInvariant();
        if (token.Length <= 3 || paroleDaIgnorare.Contains(token))
            continue;

        if (indirizzoResidenza.Contains(token, StringComparison.OrdinalIgnoreCase))
        {
            return indirizzoResidenza;
        }
    }

    // 4. Nessuna corrispondenza significativa trovata → mantengo l'indirizzo originale
    return indirizzoUbicazione;
}


}