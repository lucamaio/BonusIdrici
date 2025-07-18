using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Dichiarante;
//using Atto;
using leggiCSV;

public class CSVReader
{
    private const char CsvDelimiter = ';';
    private const string PresenzaPodValue = "SI";
    private const string DateFormat = "dd/MM/yyyy";

    public static DatiCsvCompilati LeggiFileCSV(string percorsoFile)
    {
        var datiComplessivi = new DatiCsvCompilati();

        try
        {
            var righe = File.ReadAllLines(percorsoFile).Skip(1);

            int rigaCorrente = 1;
            foreach (var riga in righe)
            {
                rigaCorrente++;
                if (string.IsNullOrWhiteSpace(riga)) continue;

                var campi = riga.Split(CsvDelimiter);

                // if (campi.Length < 16)
                // {
                //     Console.WriteLine($"Attenzione: Riga {rigaCorrente} malformata, saltata. Numero di campi: {campi.Length}");
                //     continue;
                // }

                // int idAttoOriginaleCsv; // Nuovo nome per l'ID dal CSV
                // if (!int.TryParse(campi[0], out idAttoOriginaleCsv))
                // {
                //     Console.WriteLine($"Errore: Riga {rigaCorrente}, impossibile convertire 'id' ({campi[0]}) in int. Sarà 0.");
                //     idAttoOriginaleCsv = 0;
                // }

                // long codBonusIdrico;
                // if (!long.TryParse(campi[1], out codBonusIdrico))
                // {
                //     Console.WriteLine($"Errore: Riga {rigaCorrente}, impossibile convertire 'codBonusIdrico' ({campi[1]}) in long. Sarà 0.");
                //     codBonusIdrico = 0;
                // }

                // int annoAtto;
                // if (!int.TryParse(campi[6], out annoAtto))
                // {
                //     Console.WriteLine($"Errore: Riga {rigaCorrente}, impossibile convertire 'anno' ({campi[6]}) in int. Sarà 0.");
                //     annoAtto = 0;
                // }

                // DateTime dataInizio;
                // if (!DateTime.TryParseExact(campi[7], DateFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dataInizio))
                // {
                //     Console.WriteLine($"Errore: Riga {rigaCorrente}, impossibile convertire 'dataInizio' ({campi[7]}) in data (formato atteso: {DateFormat}). Sarà DateTime.MinValue.");
                //     dataInizio = DateTime.MinValue;
                // }

                // DateTime dataFine;
                // if (!DateTime.TryParseExact(campi[8], DateFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dataFine))
                // {
                //     Console.WriteLine($"Errore: Riga {rigaCorrente}, impossibile convertire 'dataFine' ({campi[8]}) in data (formato atteso: {DateFormat}). Sarà DateTime.MinValue.");
                //     dataFine = DateTime.MinValue;
                // }

                // bool presenzaPod = GetPresenzaPodCaseInsensitive(campi[14]);

                var dichiarante = new Dichiarante.Dichiarante
                {
                    Cognome = campi[0].Trim(),
                    Nome = campi[1].Trim(),
                    CodiceFiscale = campi[2].Trim(),
                    Sesso = campi[3].Trim(),
                    DataNascita = campi[4].Trim(),
                    ComuneNascita = campi[5].Trim(),
                    IndirizzoResidenza = campi[7].Trim(),
                    NumeroCivico = campi[8].Trim(),
                    Parentela = campi[10].Trim(),
                    CodiceFamiglia = campi[11].Trim(),
                    NumeroComponenti = campi[13].Trim(),
                    NomeEnte = campi[16].Trim(),
                    CodiceFiscaleIntestatarioScheda = campi[19].Trim()
                };
                datiComplessivi.Dichiaranti.Add(dichiarante);

                //     var atto = new Atto.Atto
                //     {
                //         // NON assegnare id qui, lascialo a 0 per l'auto-generazione del DB.
                //         // id = idAtto, // Rimuovi o commenta questa riga
                //         OriginalCsvId = idAttoOriginaleCsv, // Assegna l'ID del CSV alla nuova proprietà
                //         codBonusIdrico = codBonusIdrico,
                //         Anno = annoAtto,
                //         DataInizio = dataInizio,
                //         DataFine = dataFine,
                //         PRESENZA_POD = presenzaPod
                //     };
                //     datiComplessivi.Atti.Add(atto);
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

    public static List<DateTime?> LeggiDateCSV(string percorsoFile)
    {
        DateTime? dataInizio = null;
        DateTime? dataFine = null;
        List<DateTime?> date = new List<DateTime?>();

        try
        {
            var righe = File.ReadAllLines(percorsoFile);

            if (righe.Length <= 1)
            {
                Console.WriteLine("Il file CSV è vuoto o contiene solo l'intestazione.");
                return date;
            }

            string primaRigaDati = righe[1];

            if (string.IsNullOrWhiteSpace(primaRigaDati))
            {
                Console.WriteLine("La prima riga di dati nel file CSV è vuota.");
                return date;
            }

            var campi = primaRigaDati.Split(CsvDelimiter);

            if (campi.Length <= 8)
            {
                Console.WriteLine($"Errore: La prima riga di dati non contiene abbastanza campi per le date di validità (campi 7 e 8). Trovati {campi.Length}, attesi almeno 9.");
                return date;
            }

            if (DateTime.TryParseExact(campi[7].Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDataInizio))
            {
                dataInizio = parsedDataInizio;
            }
            else
            {
                Console.WriteLine($"Avviso: Impossibile convertire '{campi[7].Trim()}' in DataInizio nel formato '{DateFormat}'. Impostato a NULL.");
            }

            if (DateTime.TryParseExact(campi[8].Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDataFine))
            {
                dataFine = parsedDataFine;
            }
            else
            {
                Console.WriteLine($"Avviso: Impossibile convertire '{campi[8].Trim()}' in DataFine nel formato '{DateFormat}'. Impostato a NULL.");
            }

            date.Add(dataInizio);
            date.Add(dataFine);

            return date;
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Errore: Il file CSV non è stato trovato al percorso specificato: {percorsoFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore generico durante la lettura del file CSV: {ex.Message}");
        }

        return date;
    }

    public static DatiCsvCompilati LeggiFilePhiranaCSV(string percorsoFile)
    {
        var datiComplessivi = new DatiCsvCompilati();

        try
        {
            var righe = File.ReadAllLines(percorsoFile).Skip(1);

            int rigaCorrente = 1;
            // Console.WriteLine($"Inizio lettura del file CSV: {percorsoFile}");
            // Console.WriteLine($"Numero di righe da elaborare: {righe.Count()}");
            // Console.WriteLine($"Numero di righe {righe}");
            // Console.WriteLine($"Formato atteso per le date: {DateFormat}");
                
                foreach (var riga in righe)
                {
                    Console.WriteLine($"Inizio lettura del file CSV: {percorsoFile}");
                    Console.WriteLine($"Numero di righe da elaborare: {righe.Count()}");
                    Console.WriteLine($"Formato atteso per le date: {DateFormat}");

                    rigaCorrente++;

                    if (string.IsNullOrWhiteSpace(riga)) continue;

                    var campi = riga.Split(CsvDelimiter);
                    // Il campo codiceFiscale è a indice 38, quindi ci servono almeno 39 campi.
                    if (campi.Length < 39)
                    {
                        Console.WriteLine($"Attenzione: Riga {rigaCorrente} malformata, saltata. Numero di campi: {campi.Length}. Attesi almeno 39.");
                        continue;
                    }

                    // Controllo se l'utenza presenta il codice Fiscale
                    // Usiamo campi[38] qui
                    if (string.IsNullOrWhiteSpace(campi[38])) // CODICE FISCALE O PARTITA IVA
                    {
                        Console.WriteLine($"Attenzione: Codice Fiscale mancante, saltata. Riga {rigaCorrente}");
                        continue;
                    }

                    // Controllo se la matricola del contatore è presente
                    if (string.IsNullOrWhiteSpace(campi[12]) || string.IsNullOrWhiteSpace(campi[0].Trim()))
                    {
                        Console.WriteLine($"Attenzione: Matricola o id Acquedotto mancante, saltata. Riga {rigaCorrente}");
                        continue;
                    }

                    // Controllo se i campi nomi e cognome sono presenti
                    if (string.IsNullOrWhiteSpace(campi[31]) || string.IsNullOrWhiteSpace(campi[32]))
                    {
                        Console.WriteLine($"Attenzione: Nome o Cognome mancante, saltata. Riga {rigaCorrente}");
                        continue;
                    }

                    //Console.WriteLine($"Riga {rigaCorrente}: Elaborazione utenza con idAcquedotto: {campi[0].Trim()}, MatricolaContatore: {campi[12].Trim()}, CodiceFiscale: {campi[38].Trim()}, Cognome: {campi[31].Trim()}, Nome: {campi[32].Trim()}, PeriodoIniziale: {campi[13].Trim()}, PeriodoFinale: {campi[14].Trim()}, NumeroCivico: {campi[16].Trim()}, IndirizzoUbicazione: {campi[15].Trim()}, SubUbicazione: {campi[17].Trim()}, ScalaUbicazione: {campi[18].Trim()}, Piano: {campi[19].Trim()}, Interno: {campi[20].Trim()}, TipoUtenza: {campi[26].Trim()}");
                    Console.WriteLine($"Riga {rigaCorrente}: Elaborazione utenza con idAcquedotto: {campi[0].Trim()}, MatricolaContatore: {campi[12].Trim()}, CodiceFiscale: {campi[38].Trim()}, Cognome: {campi[31].Trim()}, Nome: {campi[32].Trim()}, PeriodoIniziale: {campi[13].Trim()}, PeriodoFinale: {campi[14].Trim()}, NumeroCivico: {formataNumeroCivico(campi[16].Trim())}, IndirizzoUbicazione: {campi[15].Trim()}, SubUbicazione: {campi[17].Trim()}, ScalaUbicazione: {campi[18].Trim()}, Piano: {campi[19].Trim()}, Interno: {campi[20].Trim()}, TipoUtenza: {campi[26].Trim()}");
                    var utenza = new BonusIdrici2.Models.UtenzaIdrica
                    {
                        idAcquedotto = campi[0].Trim(),
                        stato = int.TryParse(campi[9].Trim(), out int stato) ? stato : 0, // Default a 0 se non convertibile
                        // CORREZIONE: Converti la stringa del campo 13 in DateTime?
                        periodoIniziale = campi[13].Trim(),
                        periodoFinale = campi[14].Trim(),
                        matricolaContatore = campi[12].Trim(),
                        indirizzoUbicazione = campi[15].Trim(),
                        numeroCivico = campi[16].Trim(), // Assicurati che formataNumeroCivico sia accessibile o spostato
                        subUbicazione = campi[17].Trim(),
                        scalaUbicazione = campi[18].Trim(),
                        piano = campi[19].Trim(),
                        interno = campi[20].Trim(),
                        tipoUtenza = campi[26].Trim(),
                        cognome = campi[32].Trim(),
                        nome = campi[33].Trim(),
                        codiceFiscale = campi[36].Trim(), // AGGIORNATO: da 35 a 38
                    };

                    datiComplessivi.UtenzeIdriche.Add(utenza);

                    // CORREZIONE STAMPA: Gestisci correttamente le date nullable
                    // Console.WriteLine($"Riga {rigaCorrente}: UtenzaIdrica: idAcquedotto: {utenza.idAcquedotto}, MatricolaContatore: {utenza.matricolaContatore}, CodiceFiscale: {utenza.codiceFiscale}, Cognome: {utenza.cognome}, Nome: {utenza.nome}, PeriodoIniziale: {(utenza.periodoIniziale)}, PeriodoFinale: {(utenza.periodoFinale ?? "N/A")}, NumeroCivico: {utenza.numeroCivico}");
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

    private static string formataMatricola(string stringa)
    {
        string matricola = stringa.Trim();
        if (matricola.Contains("/") || matricola.Contains("-"))
        {
            // Rimuove gli spazi e i caratteri speciali come / e -
            matricola = matricola.Replace("/", "").Replace("-", "");
        }
        return matricola;
    }
    
     private static string formataNumeroCivico(string stringa)
    {
        string numero_civico=stringa.Trim();
        if (numero_civico.Equals("0"))
        {
            return "SNC";
        }
        return numero_civico;
    }
}
