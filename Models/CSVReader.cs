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

    public static DatiCsvCompilati LoadAnagrafe(string percorsoFile)
    {
        var datiComplessivi = new DatiCsvCompilati();
        FileLog logFile = new FileLog($"Elaborazione_Anagrafe.log");
        List<string> errori = new List<string>();
        try
        {
            var righe = File.ReadAllLines(percorsoFile).Skip(1);
            int rigaCorrente = 1;
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

                // i) Verifico se il campo Nome Ente è presente

                if (string.IsNullOrEmpty(rimuoviVirgolette(campi[16])))
                {
                    errori.Add($"Attenzione: Nome Ente mancante, saltata. Riga {rigaCorrente}");
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
                    NomeEnte = rimuoviVirgolette(campi[16]).ToUpper(),
                    CodiceFiscaleIntestatarioScheda = rimuoviVirgolette(campi[19])
                };

                datiComplessivi.Dichiaranti.Add(dichiarante);
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

    public static DatiCsvCompilati LeggiFilePhirana(string percorsoFile)
    {
        var datiComplessivi = new DatiCsvCompilati();
        FileLog logFile = new FileLog($"Lettura_phirana.log");
        List<string> errori = new List<string>();
        List <string> warning = new List<string>();

        try
        {
            var righe = File.ReadAllLines(percorsoFile).Skip(1);

            int rigaCorrente = 1;
            logFile.LogInfo($"Nuovo caricamento dati phirana per il Comune di ...");
            logFile.LogInfo($"Numero di righe da elaborare: {righe.Count()} ");

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

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[36])))
                {
                    if (rimuoviVirgolette(campi[34]) != "D")
                    {
                        errori.Add($"Attenzione : Codice Fiscale mancante, saltata. | Riga {rigaCorrente} | Nominativo: {rimuoviVirgolette(campi[32])} {rimuoviVirgolette(campi[33])}");
                    }
                    else
                    {
                        warning.Add($"Attenzione: Codice Fiscale della ditta {rimuoviVirgolette(campi[32])} non presente. (Questo non è un errore, ma una segnalazione).");
                    }
                    error = true;
                }
                else if (rimuoviVirgolette(campi[36]).Length != 16)
                {
                    // Se il codice fiscale non è lungo 16 caratteri e non è un codice di tipo "D" (dichiarazione)
                    if (rimuoviVirgolette(campi[34]) != "D")
                    {
                        errori.Add($"Attenzione : Codice Fiscale mal formato, saltata. | Riga {rigaCorrente} | Nominativo: {rimuoviVirgolette(campi[32])} {rimuoviVirgolette(campi[33])}");
                    }
                    else
                    {
                        warning.Add($"Attenzione: Codice Fiscale della ditta {rimuoviVirgolette(campi[32])} < 16 caratteri . (Questo non è un errore, ma una segnalazione).");
                    }
                    error = true;
                }

                // e) Controllo se la matricola del contatore è presente

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[12])))
                {
                    errori.Add($"Attenzione: Matricola mancante, saltata. Riga {rigaCorrente} | Nominativo: {rimuoviVirgolette(campi[32])} {rimuoviVirgolette(campi[33])} | Codice Fiscale: {rimuoviVirgolette(campi[36])}");
                    error = true;
                }

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[0])))
                {
                    errori.Add($"Attenzione: Id Acquedotto mancante, saltata. Riga {rigaCorrente}  | Nominativo: {rimuoviVirgolette(campi[32])} {rimuoviVirgolette(campi[33])} | Codice Fiscale: {rimuoviVirgolette(campi[36])}");
                    error = true;
                }

                // f) Controllo se i campi nomi e cognome sono presenti

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[31])) || string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[32])))
                {
                    errori.Add($"Attenzione: Nome o Cognome mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {rimuoviVirgolette(campi[0])} | Matricola Contatore: {rimuoviVirgolette(campi[12])}");
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

                // Verifico se il campo numero civico è presente

                if (string.IsNullOrWhiteSpace(FormattaNumeroCivico(campi[16])))
                {
                    errori.Add($"Attenzione: Numero civico mancante, saltata. | Riga {rigaCorrente} | idAcquedotto : {rimuoviVirgolette(campi[0])} | Matricola Contatore: {rimuoviVirgolette(campi[12])}");
                    error = true;
                }

                // Controllo se sono presenti Errori

                if (error)
                {
                    continue; // Salta la riga se ci sono errori
                }

                // Creo una istanza di Utenza Idrica

                var utenza = new UtenzaIdrica
                {
                    idAcquedotto = rimuoviVirgolette(campi[0]),
                    stato = int.TryParse(rimuoviVirgolette(campi[9]), out int stato) ? stato : 0,
                    periodoIniziale = ConvertiData(rimuoviVirgolette(campi[13])),
                    periodoFinale = ConvertiData(rimuoviVirgolette(campi[14])),
                    matricolaContatore = rimuoviVirgolette(campi[12]).ToUpper(),
                    indirizzoUbicazione = rimuoviVirgolette(campi[15]).ToUpper(),
                    numeroCivico = FormattaNumeroCivico(campi[16]).ToUpper(),
                    subUbicazione = rimuoviVirgolette(campi[17]).ToUpper(),
                    scalaUbicazione = rimuoviVirgolette(campi[18]),
                    piano = rimuoviVirgolette(campi[19]),
                    interno = rimuoviVirgolette(campi[20]),
                    tipoUtenza = rimuoviVirgolette(campi[26]).ToUpper(),
                    cognome = rimuoviVirgolette(campi[32]).ToUpper(),
                    nome = rimuoviVirgolette(campi[33]).ToUpper(),
                    codiceFiscale = rimuoviVirgolette(campi[36]).ToUpper(),
                };

                datiComplessivi.UtenzeIdriche.Add(utenza);

                // Stampa di debug
                // errori.Add($"Riga {rigaCorrente}: UtenzaIdrica: idAcquedotto: {utenza.idAcquedotto}, MatricolaContatore: {utenza.matricolaContatore}, CodiceFiscale: {utenza.codiceFiscale},\n PeriodoIniziale: {(utenza.periodoIniziale)}, PeriodoFinale: {(utenza.periodoFinale)}, TipoUtenza: {utenza.tipoUtenza}, IndirizzoUbicazione: {utenza.indirizzoUbicazione}, NumeroCivico: {utenza.numeroCivico}, Nome: {utenza.nome}, Cognome: {utenza.cognome}");
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
                foreach (var errore in warning)
                {
                    logFile.LogWarning(errore);
                }
            }
            else
            {
                logFile.LogInfo("Elaborazione completata senza warning.");
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


    public static DatiCsvCompilati LeggiFileINPS(string percorsoFile, BonusIdrici2.Data.ApplicationDbContext context, int selectedEnteId)
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
                var nomeEnte = ente.nome;
                var codiceFiscaleEnte = ente.CodiceFiscale;
                var istatEnte = ente.istat;
                var capEnte = ente.Cap;

                // 2.b) Verifico se i campi ISTAT, CAP e provincia corrispondono a l'ente selezionato il quale gestisce le utenze idriche

                if (!(istatEnte != istatAbitazione || capEnte != capAbitazione || provinciaAbitazione != ente.Provincia))
                {
                    // 2.c) se i campi corispondono verifico se il richiedente è residente nel comune selezionato
                    var dichiarantiFiltratiPerNomeEnte = dichiaranti.Where(s => s.CodiceFiscale == codiceFiscale && s.NomeEnte == nomeEnte).ToList();
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
                                    var dichiaranteFamigliare = dichiaranti.Where(s => s.CodiceFiscale == codFisc && s.NomeEnte == nomeEnte).ToList();
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
            if(errori.Count > 0)
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
    

    private static (string esito, int? idFornitura) verificaEsisistenzaFornitura(string codiceFiscale, int selectedEnteId, BonusIdrici2.Data.ApplicationDbContext context, string IndirizzoResidenza, string NumeroCivico)
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

}
