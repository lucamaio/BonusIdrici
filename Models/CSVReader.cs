using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions; // Necessario per la classe Regex
using BonusIdrici2.Data;
//using Atto;
using leggiCSV;
using Org.BouncyCastle.Crypto.Digests;
using ZstdSharp.Unsafe;
using Org.BouncyCastle.Bcpg;

public class CSVReader
{
    private const char CsvDelimiter = ';';
    private const string PresenzaPodValue = "SI";
    private const string DateFormat = "dd/MM/yyyy";

    public static DatiCsvCompilati LoadAnagrafe(string percorsoFile)
    {
        var datiComplessivi = new DatiCsvCompilati();

        try
        {
            var righe = File.ReadAllLines(percorsoFile).Skip(1);

            int errori = 0;
            int rigaCorrente = 1;
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
                    Console.WriteLine($"Attenzione: Riga {rigaCorrente} malformata, saltata. Numero di campi: {campi.Length}. Attesi almeno 39.");
                    continue;
                }

                // c) Verifico che il campo cognome è valido 

                if (string.IsNullOrEmpty(rimuoviVirgolette(campi[0])))
                {
                    Console.WriteLine($"Attenzione: Cognome mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // d) Verifico che il campo nome è valido

                if (string.IsNullOrEmpty(rimuoviVirgolette(campi[1])))
                {
                    Console.WriteLine($"Attenzione: Nome mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // e) Verifico che il Codice Fiscale è valido

                if (string.IsNullOrEmpty(rimuoviVirgolette(campi[2])) || rimuoviVirgolette(campi[2]).Length != 16)
                {
                    Console.WriteLine($"Attenzione: Codice Fiscale mancante o mal formato, saltata. Riga {rigaCorrente} {rimuoviVirgolette(campi[2]).Length}");
                    errori++;
                    error = true;
                }

                // f) Verifico il campo sesso se è valido

                if (string.IsNullOrEmpty(rimuoviVirgolette(campi[3])) && (rimuoviVirgolette(campi[3]).ToUpper() != "M") && (rimuoviVirgolette(campi[3]).ToUpper() != "F"))
                {
                    Console.WriteLine($"Attenzione: Sesso mancante o mal formato, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // g) Verifico se il campo indirizzo residente è presente

                if (string.IsNullOrEmpty(rimuoviVirgolette(campi[7])))
                {
                    Console.WriteLine($"Attenzione: Indirizzo residenza mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // h) Verifico se il campo numero civico è presente

                if (string.IsNullOrWhiteSpace(FormattaNumeroCivico(campi[8])))
                {
                    Console.WriteLine($"Attenzione: Numero civico mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // i) Verifico se il campo Nome Ente è presente

                 if (string.IsNullOrEmpty(rimuoviVirgolette(campi[16])))
                {
                    Console.WriteLine($"Attenzione: Nome Ente mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // Controllo se sono presenti Errori

                  if (error)
                {
                    continue; // Salta la riga se ci sono errori
                }

                // Creo una istanza di Dichiarante

                var dichiarante = new BonusIdrici2.Models.Dichiarante
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
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Errore: Il file CSV non è stato trovato al percorso specificato: {percorsoFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore generico durante la lettura del file CSV: {ex.Message}");
        }

        return datiComplessivi;
    }

    // public static List<DateTime?> LeggiDateCSV(string percorsoFile)
    // {
    //     DateTime? dataInizio = null;
    //     DateTime? dataFine = null;
    //     List<DateTime?> date = new List<DateTime?>();

    //     try
    //     {
    //         var righe = File.ReadAllLines(percorsoFile);

    //         if (righe.Length <= 1)
    //         {
    //             Console.WriteLine("Il file CSV è vuoto o contiene solo l'intestazione.");
    //             return date;
    //         }

    //         string primaRigaDati = righe[1];

    //         if (string.IsNullOrWhiteSpace(primaRigaDati))
    //         {
    //             Console.WriteLine("La prima riga di dati nel file CSV è vuota.");
    //             return date;
    //         }

    //         var campi = primaRigaDati.Split(CsvDelimiter);

    //         if (campi.Length <= 8)
    //         {
    //             Console.WriteLine($"Errore: La prima riga di dati non contiene abbastanza campi per le date di validità (campi 7 e 8). Trovati {campi.Length}, attesi almeno 9.");
    //             return date;
    //         }

    //         if (DateTime.TryParseExact(campi[7].Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDataInizio))
    //         {
    //             dataInizio = parsedDataInizio;
    //         }
    //         else
    //         {
    //             Console.WriteLine($"Avviso: Impossibile convertire '{campi[7].Trim()}' in DataInizio nel formato '{DateFormat}'. Impostato a NULL.");
    //         }

    //         if (DateTime.TryParseExact(campi[8].Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDataFine))
    //         {
    //             dataFine = parsedDataFine;
    //         }
    //         else
    //         {
    //             Console.WriteLine($"Avviso: Impossibile convertire '{campi[8].Trim()}' in DataFine nel formato '{DateFormat}'. Impostato a NULL.");
    //         }

    //         date.Add(dataInizio);
    //         date.Add(dataFine);

    //         return date;
    //     }
    //     catch (FileNotFoundException)
    //     {
    //         Console.WriteLine($"Errore: Il file CSV non è stato trovato al percorso specificato: {percorsoFile}");
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"Errore generico durante la lettura del file CSV: {ex.Message}");
    //     }

    //     return date;
    // }

    public static DatiCsvCompilati LeggiFilePhirana(string percorsoFile)
    {
        var datiComplessivi = new DatiCsvCompilati();

        try
        {
            var righe = File.ReadAllLines(percorsoFile).Skip(1);

            int rigaCorrente = 1;
            int errori = 0;
            Console.WriteLine($"Inizio lettura del file CSV: {percorsoFile}");
            Console.WriteLine($"Numero di righe da elaborare: {righe.Count()}");

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
                    Console.WriteLine($"Attenzione: Riga {rigaCorrente} malformata, saltata. Numero di campi: {campi.Length}. Attesi almeno 39.");
                    continue;
                }

                // c) Verifico che esiste il campo idAcquedotto è presente

                if (string.IsNullOrEmpty(rimuoviVirgolette(campi[0])))
                {
                    Console.WriteLine($"Attenzione: Id Acquedotto mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // d) Controllo se il campo Codice Fiscale è valido è != null

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[36])) || rimuoviVirgolette(campi[36]).Length != 16) // CODICE FISCALE O PARTITA IVA
                {
                    Console.WriteLine($"Attenzione: Codice Fiscale mancante o malformato, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // e) Controllo se la matricola del contatore è presente

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[12])) || string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[0].Trim())))
                {
                    Console.WriteLine($"Attenzione: Matricola o id Acquedotto mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // f) Controllo se i campi nomi e cognome sono presenti

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[31])) || string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[32])))
                {
                    Console.WriteLine($"Attenzione: Nome o Cognome mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // g) Controllo se il campo periodoIniziale è presente

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[13])))
                {
                    Console.WriteLine($"Attenzione: Periodo iniziale mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // h) Verifico se il campo tipo utenza è presente

                 if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[26])))
                {
                    Console.WriteLine($"Attenzione: Tipo Utenza mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }
                
                // i) Verifico se il campo indirizzo Ubicazione è presente

                 if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[15])))
                {
                    Console.WriteLine($"Attenzione: Indirizzo ubicazione mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // Verifico se il campo numero civico è presente

                if (string.IsNullOrWhiteSpace(FormattaNumeroCivico(campi[16])))
                {
                    Console.WriteLine($"Attenzione: Numero civico mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }
                Console.WriteLine("Pre Error Control");
                // Controllo se sono presenti Errori

                if (error)
                {
                    continue; // Salta la riga se ci sono errori
                }

                Console.WriteLine("POST Error control");

                // Creo una istanza di Utenza Idrica

                var utenza = new BonusIdrici2.Models.UtenzaIdrica
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
                //Console.WriteLine($"Riga {rigaCorrente}: UtenzaIdrica: idAcquedotto: {utenza.idAcquedotto},  CodiceFiscale: {utenza.codiceFiscale},\n PeriodoIniziale: {(utenza.periodoIniziale)}, PeriodoFinale: {(utenza.periodoFinale)},\n");
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Errore: Il file CSV non è stato trovato al percorso specificato: {percorsoFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore generico durante la lettura del file CSV: {ex.Message}");
        }

        return datiComplessivi;
    }


    public static DatiCsvCompilati LeggiFileINPS(string percorsoFile, BonusIdrici2.Data.ApplicationDbContext context, int selectedEnteId)
    {
        var datiComplessivi = new DatiCsvCompilati();

        try
        {
            var righe = File.ReadAllLines(percorsoFile).Skip(1);

            int rigaCorrente = 1;
            int errori = 0;
            Console.WriteLine($"Numero di righe da elaborare: {righe.Count()}");
            var dichiaranti = context.Dichiaranti.ToList();
            //Console.WriteLine($"Dichiaranti presenti: {dichiaranti.Count}");

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
                    Console.WriteLine($"Attenzione: Riga {rigaCorrente} malformata, saltata. Numero di campi: {campi.Length}. Attesi almeno 15.");
                    errori++;
                    error = true;
                }

                // Verifico se il campo idAto è presente e nonn vuoto
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[0])))
                {
                    Console.WriteLine($"Attenzione: ID_ATO mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // Verifico se il campo codice_bonus è presente, non vuoto è ha una lunhezza di 15 carratteri

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[1])) || rimuoviVirgolette(campi[1]).Length != 15)
                {
                    Console.WriteLine($"Attenzione: Codice Bonus mancante o non valido, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // Verifico se il campo Codice Fiscale è presente e non vuoto

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[2])))
                {
                    Console.WriteLine($"Attenzione: Codice Fiscale mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // Verifico se il campo NOME_DICHIARANTE e COGNOME_DICHIARANTE sono presenti e non vuoti
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[3])) || string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[4])))
                {
                    Console.WriteLine($"Attenzione: Nome o Cognome mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // Verifico se il campo Anno_validità è presente e non vuoto

                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[6])))
                {
                    Console.WriteLine($"Attenzione: Anno di validità mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // verifico se il campo Data_inizio_validità è presente e non vuoto
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[7])))
                {
                    Console.WriteLine($"Attenzione: Data di inizio validità mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // Verifico se il campo Data_fine_validità è presente e non vuoto
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[8])))
                {
                    Console.WriteLine($"Attenzione: Data di fine validità mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // Verifico se il campo indirizzo_abitazione è presente e non vuoto
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[9])))
                {
                    Console.WriteLine($"Attenzione: Indirizzo abitazione mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // Verifico se il campo numero_civico è presente e non vuotO
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[10])))
                {
                    Console.WriteLine($"Attenzione: Numero civico mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // Verifico se il campo iSTAT è presente e non vuoto
                string istat = rimuoviVirgolette(campi[11]);

                if (string.IsNullOrWhiteSpace(istat) || istat.Length != 6)
                {
                    Console.WriteLine($"Attenzione: ISTAT mancante o malformata, saltata. Riga {rigaCorrente}");
                    errori++;
                }

                // VERIFICO Se il campo cap è presente e non vuoto
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[12])) || rimuoviVirgolette(campi[12]).Length != 5)
                {
                    Console.WriteLine($"Attenzione: CAP mancante, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // vrifico se il campo provincia_abitazione è presente e non vuoto
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[13])) || rimuoviVirgolette(campi[13]).Length != 2)
                {
                    Console.WriteLine($"Attenzione: Provincia abitazione mancante o malformata, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // Verifico se il campo Presenza_POD è presente ed è valido
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[14])) ||
                    (!rimuoviVirgolette(campi[14]).Equals("SI", StringComparison.OrdinalIgnoreCase) &&
                     !rimuoviVirgolette(campi[14]).Equals("NO", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine($"Attenzione: Presenza POD mancante o non valida, saltata. Riga {rigaCorrente}");
                    errori++;
                    error = true;
                }

                // verifdico se il campo n_componenti è presente e non vuoto
                if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[15])))
                {
                    Console.WriteLine($"Attenzione: Numero componenti mancante, saltata. Riga {rigaCorrente}");
                    errori++;
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
                    Console.WriteLine($"Attenzione: Nessun ente trovato con l'ID selezionato {selectedEnteId}. Riga {rigaCorrente}");
                    errori++;
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
                var report = new BonusIdrici2.Models.Report
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
                    DataCreazione=dataCreazione
                };

                datiComplessivi.reports.Add(report);

                Console.WriteLine($"Riga {rigaCorrente}: Report creato: idAto: {report.idAto}, CodiceBonus: {report.codiceBonus}, CodiceFiscale: {report.codiceFiscale}, NomeDichiarante: {report.nomeDichiarante}, CognomeDichiarante: {report.cognomeDichiarante}, Esito: {report.esitoStr}, Esito numerico: {report.esito}, DataInizioValidita: {report.dataInizioValidita}, DataFineValidita: {report.dataFineValidita}");
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Errore: Il file CSV non è stato trovato al percorso specificato: {percorsoFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore generico durante la lettura del file CSV: {ex.Message}");
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
        // Se vuoi includere lo spazio come delimitatore per la "parte iniziale":
        // if (indiceSpazio != -1)
        // {
        //     firstDelimiterIndex = Math.Min(firstDelimiterIndex, indiceSpazio);
        // }


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
           // Console.WriteLine("CASO 1 di conversione!");
            return parsedDate;
        }

        // Se hai altri formati possibili nel CSV (es. "aaaa-MM-gg"), aggiungili qui:
        if (DateTime.TryParseExact(dataStringa, "yyyy-MM-dd", new CultureInfo("it-IT"), DateTimeStyles.None, out parsedDate))
        {
            //Console.WriteLine("CASO 2 di conversione!");
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
