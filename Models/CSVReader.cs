using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Dichiarante;
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

    // public static DatiCsvCompilati LeggiFileCSV(string percorsoFile)
    // {
    //     var datiComplessivi = new DatiCsvCompilati();

    //     try
    //     {
    //         var righe = File.ReadAllLines(percorsoFile).Skip(1);

    //         int rigaCorrente = 1;
    //         foreach (var riga in righe)
    //         {
    //             rigaCorrente++;
    //             if (string.IsNullOrWhiteSpace(riga)) continue;

    //             var campi = riga.Split(CsvDelimiter);

    //             // if (campi.Length < 16)
    //             // {
    //             //     Console.WriteLine($"Attenzione: Riga {rigaCorrente} malformata, saltata. Numero di campi: {campi.Length}");
    //             //     continue;
    //             // }

    //             // int idAttoOriginaleCsv; // Nuovo nome per l'ID dal CSV
    //             // if (!int.TryParse(campi[0], out idAttoOriginaleCsv))
    //             // {
    //             //     Console.WriteLine($"Errore: Riga {rigaCorrente}, impossibile convertire 'id' ({campi[0]}) in int. Sarà 0.");
    //             //     idAttoOriginaleCsv = 0;
    //             // }

    //             // long codBonusIdrico;
    //             // if (!long.TryParse(campi[1], out codBonusIdrico))
    //             // {
    //             //     Console.WriteLine($"Errore: Riga {rigaCorrente}, impossibile convertire 'codBonusIdrico' ({campi[1]}) in long. Sarà 0.");
    //             //     codBonusIdrico = 0;
    //             // }

    //             // int annoAtto;
    //             // if (!int.TryParse(campi[6], out annoAtto))
    //             // {
    //             //     Console.WriteLine($"Errore: Riga {rigaCorrente}, impossibile convertire 'anno' ({campi[6]}) in int. Sarà 0.");
    //             //     annoAtto = 0;
    //             // }

    //             // DateTime dataInizio;
    //             // if (!DateTime.TryParseExact(campi[7], DateFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dataInizio))
    //             // {
    //             //     Console.WriteLine($"Errore: Riga {rigaCorrente}, impossibile convertire 'dataInizio' ({campi[7]}) in data (formato atteso: {DateFormat}). Sarà DateTime.MinValue.");
    //             //     dataInizio = DateTime.MinValue;
    //             // }

    //             // DateTime dataFine;
    //             // if (!DateTime.TryParseExact(campi[8], DateFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dataFine))
    //             // {
    //             //     Console.WriteLine($"Errore: Riga {rigaCorrente}, impossibile convertire 'dataFine' ({campi[8]}) in data (formato atteso: {DateFormat}). Sarà DateTime.MinValue.");
    //             //     dataFine = DateTime.MinValue;
    //             // }

    //             // bool presenzaPod = GetPresenzaPodCaseInsensitive(campi[14]);

    //             var dichiarante = new Dichiarante.Dichiarante
    //             {
    //                 Cognome = campi[0].Trim(),
    //                 Nome = campi[1].Trim(),
    //                 CodiceFiscale = campi[2].Trim(),
    //                 Sesso = campi[3].Trim(),
    //                 DataNascita = campi[4].Trim(),
    //                 ComuneNascita = campi[5].Trim(),
    //                 IndirizzoResidenza = campi[7].Trim(),
    //                 NumeroCivico = campi[8].Trim(),
    //                 Parentela = campi[10].Trim(),
    //                 // CodiceFamiglia = campi[11].Trim(),
    //                 // NumeroComponenti = campi[13].Trim(),
    //                 NomeEnte = campi[16].Trim(),
    //                 // CodiceFiscaleIntestatarioScheda = campi[19].Trim()
    //             };
    //             datiComplessivi.Dichiaranti.Add(dichiarante);

    //             //     var atto = new Atto.Atto
    //             //     {
    //             //         // NON assegnare id qui, lascialo a 0 per l'auto-generazione del DB.
    //             //         // id = idAtto, // Rimuovi o commenta questa riga
    //             //         OriginalCsvId = idAttoOriginaleCsv, // Assegna l'ID del CSV alla nuova proprietà
    //             //         codBonusIdrico = codBonusIdrico,
    //             //         Anno = annoAtto,
    //             //         DataInizio = dataInizio,
    //             //         DataFine = dataFine,
    //             //         PRESENZA_POD = presenzaPod
    //             //     };
    //             //     datiComplessivi.Atti.Add(atto);
    //         }
    //     }
    //     catch (FileNotFoundException)
    //     {
    //         Console.WriteLine($"Errore: Il file CSV non è stato trovato al percorso specificato: {percorsoFile}");
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"Errore generico durante la lettura del file CSV: {ex.Message}");
    //     }

    //     return datiComplessivi;
    // }

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

    // public static DatiCsvCompilati LeggiFilePhiranaCSV(string percorsoFile)
    // {
    //     var datiComplessivi = new DatiCsvCompilati();

    //     try
    //     {
    //         var righe = File.ReadAllLines(percorsoFile).Skip(1);

    //         int rigaCorrente = 1;
    //         // Console.WriteLine($"Inizio lettura del file CSV: {percorsoFile}");
    //         // Console.WriteLine($"Numero di righe da elaborare: {righe.Count()}");
    //         // Console.WriteLine($"Numero di righe {righe}");
    //         // Console.WriteLine($"Formato atteso per le date: {DateFormat}");

    //         foreach (var riga in righe)
    //         {
    //             Console.WriteLine($"Inizio lettura del file CSV: {percorsoFile}");
    //             Console.WriteLine($"Numero di righe da elaborare: {righe.Count()}");
    //             Console.WriteLine($"Formato atteso per le date: {DateFormat}");

    //             rigaCorrente++;

    //             if (string.IsNullOrWhiteSpace(riga)) continue;

    //             var campi = riga.Split(CsvDelimiter);
    //             // Il campo codiceFiscale è a indice 38, quindi ci servono almeno 39 campi.
    //             if (campi.Length < 39)
    //             {
    //                 Console.WriteLine($"Attenzione: Riga {rigaCorrente} malformata, saltata. Numero di campi: {campi.Length}. Attesi almeno 39.");
    //                 continue;
    //             }

    //             // Controllo se il campo Codice Fiscale è presente e non vuoto
    //             if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[36]))) // CODICE FISCALE O PARTITA IVA
    //             {
    //                 Console.WriteLine($"Attenzione: Codice Fiscale mancante, saltata. Riga {rigaCorrente}");
    //                 continue;
    //             }

    //             // Controllo se la matricola del contatore è presente
    //             if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[12])) || string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[0].Trim())))
    //             {
    //                 Console.WriteLine($"Attenzione: Matricola o id Acquedotto mancante, saltata. Riga {rigaCorrente}");
    //                 continue;
    //             }

    //             // Controllo se i campi nomi e cognome sono presenti
    //             if (string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[31])) || string.IsNullOrWhiteSpace(rimuoviVirgolette(campi[32])))
    //             {
    //                 Console.WriteLine($"Attenzione: Nome o Cognome mancante, saltata. Riga {rigaCorrente}");
    //                 continue;
    //             }

    //             var utenza = new BonusIdrici2.Models.UtenzaIdrica
    //             {
    //                 idAcquedotto = rimuoviVirgolette(campi[0].Trim()),
    //                 stato = int.TryParse(rimuoviVirgolette(campi[9].Trim()), out int stato) ? stato : 0,
    //                 periodoIniziale = ConvertiData(rimuoviVirgolette(campi[13].Trim())),
    //                 periodoFinale = ConvertiData(rimuoviVirgolette(campi[14].Trim())),
    //                 matricolaContatore = rimuoviVirgolette(campi[12].Trim()),
    //                 indirizzoUbicazione = rimuoviVirgolette(campi[15].Trim()),
    //                 numeroCivico = rimuoviVirgolette(campi[16].Trim()),
    //                 subUbicazione = rimuoviVirgolette(campi[17].Trim()),
    //                 scalaUbicazione = rimuoviVirgolette(campi[18].Trim()),
    //                 piano = rimuoviVirgolette(campi[19].Trim()),
    //                 interno = rimuoviVirgolette(campi[20].Trim()),
    //                 tipoUtenza = rimuoviVirgolette(campi[26].Trim()),
    //                 cognome = rimuoviVirgolette(campi[32].Trim()),
    //                 nome = rimuoviVirgolette(campi[33].Trim()),
    //                 codiceFiscale = rimuoviVirgolette(campi[36].Trim()),
    //             };

    //             datiComplessivi.UtenzeIdriche.Add(utenza);

    //             // CORREZIONE STAMPA: Gestisci correttamente le date nullable
    //             Console.WriteLine($"Riga {rigaCorrente}: UtenzaIdrica: idAcquedotto: {utenza.idAcquedotto},  CodiceFiscale: {utenza.codiceFiscale},\n PeriodoIniziale: {(utenza.periodoIniziale)}, PeriodoFinale: {(utenza.periodoFinale)},\n");
    //         }
    //     }
    //     catch (FileNotFoundException)
    //     {
    //         Console.WriteLine($"Errore: Il file CSV non è stato trovato al percorso specificato: {percorsoFile}");
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"Errore generico durante la lettura del file CSV: {ex.Message}");
    //     }

    //     return datiComplessivi;
    // }


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

                string idAto = rimuoviVirgolette(campi[0]).Trim();
                string codiceBonus = rimuoviVirgolette(campi[1]).Trim();
                string codiceFiscale = rimuoviVirgolette(campi[2]).Trim();

                string nomeDichiarante = rimuoviVirgolette(campi[3]).Trim();
                string cognomeDichiarante = rimuoviVirgolette(campi[4]).Trim();
                string[] codiciFiscaliFamigliari = splitCodiceFiscale(campi[5]);

                string annoValidita = rimuoviVirgolette(campi[6]).Trim();
                string dataInizioValidita = rimuoviVirgolette(campi[7]).Trim();
                string dataFineValidita = rimuoviVirgolette(campi[8]).Trim();

                string indirizzoAbitazione = rimuoviVirgolette(campi[9]).Trim();
                string numeroCivico = formataNumeroCivico(rimuoviVirgolette(campi[10]).Trim());
                string istatAbitazione = rimuoviVirgolette(campi[11]).Trim();
                string capAbitazione = rimuoviVirgolette(campi[12]).Trim();
                string provinciaAbitazione = rimuoviVirgolette(campi[13]).Trim();

                string presenzaPod = rimuoviVirgolette(campi[14]).Trim();
                string numeroComponenti = rimuoviVirgolette(campi[15]).Trim();

                // 1.c) Aggiungo dei campi aggiuntivi neccessari per la creazione del report
                string esitoStr = "No";
                string esito = "04"; // 01 = fornitura diretta, 02 = fornitura indiretta, 03 = fornitura diretta non rispetta requisiti, 04 =  fornitura indiretta non rispetta requisiti 


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

                        // var forniture = context.UtenzeIdriche.Where(s => s.codiceFiscale == codiceFiscale && s.IdEnte == selectedEnteId).ToList();
                        // if (forniture.Count == 1)
                        // {
                        //     string tipoUtenza = forniture[0].tipoUtenza;
                        //     if (tipoUtenza.Equals("UTENZA DOMESTICA", StringComparison.OrdinalIgnoreCase))
                        //     {
                        //         // 3.c) adessso verifico se l'utenza è situata nello stesso indirizzo del richiedente
                        //         string indirizzoUtenza = forniture[0].indirizzoUbicazione;
                        //         string numeroCivicoUtenza = forniture[0].numeroCivico;    // N.B Crea una funzione per formattare il numero civico come in formataNumeroCivico

                        //         if (indirizzoUtenza.Equals(dichiarantiFiltratiPerNomeEnte[0].IndirizzoResidenza, StringComparison.OrdinalIgnoreCase) &&
                        //             numeroCivicoUtenza.Equals(dichiarantiFiltratiPerNomeEnte[0].NumeroCivico, StringComparison.OrdinalIgnoreCase))
                        //         {
                        //             // 3.d) Adesso verifico se lo stato della fornitura e compresso tra 1 e 3
                        //             if (forniture[0].stato >= 1 && forniture[0].stato <= 3)
                        //             {
                        //                 // 3.e) se lo stato è compreso tra 1 e 3 allora esito è uguale a 01
                        //                 esito = "01";
                        //             }
                        //             else
                        //             {
                        //                 // 3.e.2) se lo stato non è compreso tra 1 e 3 allora esito è uguale a 03
                        //                 esito = "03";
                        //             }
                        //         }
                        //         else
                        //         {
                        //             // 3.c.2) se l'utenza non è situata nello stesso indirizzo allora esito è uguale a 03
                        //             esito = "03";
                        //         }
                        //     }
                        // }
                        // else
                        // {
                        //     // Verifico se un membro della famiglia ha una fornitura idrica diretta
                        // }

                        string esitoRestituito = verificaEsisistenzaFornitura(codiceFiscale, selectedEnteId, context, dichiarantiFiltratiPerNomeEnte[0].IndirizzoResidenza, dichiarantiFiltratiPerNomeEnte[0].NumeroCivico);

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
                                        string esitoFamigliare = verificaEsisistenzaFornitura(codFisc, selectedEnteId, context, dichiarantiFiltratiPerNomeEnte[0].IndirizzoResidenza, dichiarantiFiltratiPerNomeEnte[0].NumeroCivico);
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
                    codiceFiscale = codiceFiscale,
                    nomeDichiarante = nomeDichiarante,
                    cognomeDichiarante = cognomeDichiarante,
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
                    IdEnte = selectedEnteId,
                };

                datiComplessivi.reports.Add(report);
                
                Console.WriteLine($"Riga {rigaCorrente}: Report creato: idAto: {report.idAto}, CodiceBonus: {report.codiceBonus}, CodiceFiscale: {report.codiceFiscale}, NomeDichiarante: {report.nomeDichiarante}, CognomeDichiarante: {report.cognomeDichiarante}, Esito: {report.esitoStr}, Esito numerico: {report.esito}, DataInizioValidita: {report.dataInizioValidita}, DataFineValidita: {report.dataFineValidita}");

                // 5) salvo il report nel database

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

    private static string formataNumeroCivico(string stringa)
    {
        string numero_civico = stringa.Trim();
        if (numero_civico.Equals("0"))
        {
            return "SNC";
        }
        return numero_civico;
    }

    private static string[] splitCodiceFiscale(string codiceFiscale)
    {
        // Rimuove gli spazi e i caratteri speciali come / e -
        string formatedCodiceFiscale = codiceFiscale.Trim().Replace("\"", "");
        return formatedCodiceFiscale.Split(',');
    }

    // private static string ConvertiData(string dataStringa)
    // {
    //     // Formato atteso della stringa in input
    //     const string formatoInput = "dd/MM/yyyy";

    //     // Cultura invariante per evitare ambiguità regionali
    //     var cultura = System.Globalization.CultureInfo.InvariantCulture;

    //     // Tenta di convertire la stringa in un oggetto DateTime
    //     if (DateTime.TryParseExact(dataStringa, formatoInput, cultura, System.Globalization.DateTimeStyles.None, out DateTime data))
    //     {
    //         // Restituisce la data nel formato "yyyy-MM-dd", adatto per il database
    //         return data.ToString("yyyy-MM-dd");
    //     }
    //     else
    //     {
    //         Console.WriteLine($"Errore: Impossibile convertire '{dataStringa}' in Data.");
    //         return string.Empty; // O puoi gestire diversamente l'errore
    //     }
    // }

private static DateTime? ConvertiData(string dataStringa)
{
    const string formatoInput = "dd/MM/yyyy";
    var cultura = System.Globalization.CultureInfo.InvariantCulture;

    if (DateTime.TryParseExact(dataStringa, formatoInput, cultura, System.Globalization.DateTimeStyles.None, out DateTime data))
    {
        return data;
    }
    else
    {
        Console.WriteLine($"Errore: Impossibile convertire '{dataStringa}' in Data.");
        return null;
    }
}
    private static string verificaEsisistenzaFornitura(string codiceFiscale, int selectedEnteId, BonusIdrici2.Data.ApplicationDbContext context, string IndirizzoResidenza, string NumeroCivico)
    {
        // Verifica se il richiedente ha una fornitura idrica diretta
        var forniture = context.UtenzeIdriche.Where(s => s.codiceFiscale == codiceFiscale && s.IdEnte == selectedEnteId).ToList();
        if (forniture.Count == 1)
        {
            string tipoUtenza = forniture[0].tipoUtenza;
            if (tipoUtenza.Equals("UTENZA DOMESTICA", StringComparison.OrdinalIgnoreCase))
            {
                // 3.c) adessso verifico se l'utenza è situata nello stesso indirizzo del richiedente
                string indirizzoUtenza = forniture[0].indirizzoUbicazione;
                string numeroCivicoUtenza = forniture[0].numeroCivico;    // N.B Crea una funzione per formattare il numero civico come in formataNumeroCivico

                if (indirizzoUtenza.Equals(IndirizzoResidenza, StringComparison.OrdinalIgnoreCase) &&
                    numeroCivicoUtenza.Equals(NumeroCivico, StringComparison.OrdinalIgnoreCase))
                {
                    // 3.d) Adesso verifico se lo stato della fornitura e compresso tra 1 e 3
                    if (forniture[0].stato >= 1 && forniture[0].stato <= 3)
                    {
                        // 3.e) se lo stato è compreso tra 1 e 3 allora esito è uguale a 01
                        return "01";
                    }
                    else
                    {
                        // 3.e.2) se lo stato non è compreso tra 1 e 3 allora esito è uguale a 03
                        return "03";
                    }
                }
                else
                {
                    // 3.c.2) se l'utenza non è situata nello stesso indirizzo allora esito è uguale a 03
                    return "03";
                }
            }
        }
        return "04"; // Nessuna fornitura
    }

}
