using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Data;
using Controllers;

using leggiCSV;
using Org.BouncyCastle.Crypto.Digests;
using ZstdSharp.Unsafe;
using Org.BouncyCastle.Bcpg;
using Models;
using Mysqlx.Notice;
using Microsoft.EntityFrameworkCore;



namespace Models
{
    public class FunzioniTrasversali
    {
        public static string rimuoviVirgolette(string stringa)
        {
            string formatedString = stringa.Trim();
            if (formatedString.Contains("\""))
            {
                // Rimuove gli spazi e i caratteri speciali come / e -
                formatedString = formatedString.Replace("\"", "");
            }
            return formatedString;
        }

        public static string FormattaNumeroCivico(string stringa)
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

        public static string[] splitCodiceFiscale(string codiceFiscale)
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

        public static DateTime ConvertiData(string dataStringa, DateTime defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(dataStringa))
            {
                return defaultValue; // Restituisce un valore di default
            }

            // Formato comune italiano: "gg/MM/aaaa"
            if (DateTime.TryParseExact(dataStringa, "dd/MM/yyyy", new CultureInfo("it-IT"), DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate;
            }

            // Altro formato comune: "aaaa-MM-gg"
            if (DateTime.TryParseExact(dataStringa, "yyyy-MM-dd", new CultureInfo("it-IT"), DateTimeStyles.None, out parsedDate))
            {
                return parsedDate;
            }

            // Se nessun formato corrisponde
            Console.WriteLine($"Attenzione: Impossibile convertire la data '{dataStringa}' nel formato atteso. Verrà restituito il valore di default.");
            return defaultValue;
        }

        /*
            NOME FUNZIONE: VerificaEsistenzaFornitura()

            SCOPO:
                Questa funzione ha l’obiettivo di verificare se un soggetto ha diritto a ricevere il bonus idrico.
                In particolare, controlla se il dichiarante è associato ad una fornitura idrica valida, ovvero:
                    - situata presso lo stesso indirizzo comunicato dall’INPS;
                    - classificata come utenza domestica.
                Inoltre, se richiesto, vengono effettuati controlli anche sul numero civico.

            PARAMETRI IN INGRESSO:
                - dichiarante: dati anagrafici del soggetto da verificare;
                - selectedEnteId: identificativo dell’ente su cui eseguire l’operazione;
                - context: connessione al database per l’esecuzione delle query;
                - indirizzoINPS: indirizzo fornito dal file CSV dell’INPS, utilizzato per il confronto;
                - numeroCivico: numero civico fornito dal file CSV dell’INPS. Viene confrontato solo se confrontaCivico = true;
                - confrontaCivico: valore booleano che stabilisce se considerare o meno il campo numero civico nel confronto.

            PARAMETRI IN USCITA:
                - Esito: rappresenta l’esito della verifica, con valori da "01" a "04":
                    - "01": fornitura trovata e conforme a tutti i requisiti;
                    - "02": nessuna fornitura trovata, ma "Presenza POD" = "SI". 
                            (Nota: questo esito non viene restituito direttamente da questa funzione, 
                            poiché richiede ulteriori verifiche preliminari);
                    - "03": fornitura trovata, ma non conforme ai requisiti (es. indirizzo diverso da quello INPS, utenza non domestica);
                    - "04": nessuna fornitura trovata e "Presenza POD" = "NO".
                - ID Fornitura: identificativo della fornitura idrica trovata (può essere null se nessuna fornitura è stata individuata);
                - Messaggio: eventuale messaggio descrittivo o di avvertimento che spiega il motivo dell’esito negativo 
                            (salvato come nota nel database);
                - ID Utenza: identificativo dell’utenza salvata nel database, distinto dall’ID Fornitura.

            NOTE:
                - Il campo "Presenza POD" indica se è presente un contatore attivo.

            ARTICOLAZIONE DELLA FUNZIONE:
                1. Recupero delle forniture associate al soggetto richiedente nell’ente selezionato.
                2. Verifica del numero di forniture trovate:
                    - Se nessuna fornitura è presente → restituisco esito "04" e messaggio "Nessuna fornitura trovata per il dichiarante".
                3. Se sono presenti più forniture → controllo la corrispondenza con l’indirizzo comunicato dall’INPS.
                4. Verifica che la fornitura sia di tipo domestico. 
                    - In caso contrario → esito "03" e messaggio "Attenzione: la fornitura trovata non è di tipo 'UTENZA DOMESTICA'".
                5. Confronto l’indirizzo della fornitura con quello fornito dall’INPS ed eventualmente anche il numero civico.
                6. Se l’indirizzo non coincide, verifico la corrispondenza tramite la normalizzazione del toponimo.
                7. Se l’indirizzo o il toponimo coincidono → restituisco esito "01", ID fornitura, ID utenza ed eventuali messaggi.
                8. Se né l’indirizzo né il toponimo coincidono → restituisco esito "03", messaggio di errore, ID fornitura e ID utenza.
        */

       public static (string esito, int? idFornitura, string? messaggio, int? idUtenza) VerificaEsistenzaFornitura(Dichiarante dichiarante, int selectedEnteId, ApplicationDbContext context, string indirizzoINPS, string numeroCivicoINPS, bool confrontoCivico)
        {
            // 1. Recupera le forniture associate al dichiarante e all'ente, escludendo stati non validi (cessate/sospese: 4, 5).
            var forniture = context.UtenzeIdriche
                .Where(s => ((s.codiceFiscale == dichiarante.CodiceFiscale || (s.cognome == dichiarante.Cognome && s.nome == dichiarante.Nome && s.DataNascita == dichiarante.DataNascita)) && (s.stato != 5 && s.stato != 4)) && s.IdEnte == selectedEnteId)
                .ToList();

            UtenzaIdrica? fornitura = null;

            if (forniture.Count > 1)
            {
                // 2. Se più forniture, cerca quella all'indirizzo di residenza del dichiarante per disambiguare.
                foreach (var utenza in forniture)
                {
                    if (string.Equals(utenza.indirizzoUbicazione, dichiarante.IndirizzoResidenza, StringComparison.OrdinalIgnoreCase))
                    {
                        fornitura = utenza;
                        // Si preferisce una fornitura non cessata/sospesa, ma non si esce subito
                        if (utenza.stato != 4 && utenza.stato != 5) 
                            break;
                    }
                }
            }
            else if (forniture.Count == 1)
            {
                // 3. Se una sola fornitura, la seleziona.
                fornitura = forniture[0];
            }
            
            if(fornitura == null)
            {
                // 4. Nessuna fornitura idonea trovata: Esito "04".
                return ("04", null, "Nessuna fornitura trovata per il dichiarante.", null);
            }

            int? idFornituraTrovata = int.TryParse(fornitura.idAcquedotto, out int idF) ? idF : null;

            // 5. Verifica il tipo di utenza: deve essere "UTENZA DOMESTICA".
            if (!string.Equals(fornitura.tipoUtenza, "UTENZA DOMESTICA", StringComparison.OrdinalIgnoreCase))
            {
                return ("03", idFornituraTrovata, "Attenzione: La fornitura trovata non è di tipo 'UTENZA DOMESTICA'.", fornitura.id);
            }

            string? indirizzoUtenza = fornitura.indirizzoUbicazione;
            string? numeroCivicoUtenza = fornitura.numeroCivico;
            string message = "";

            // 6. Confronto Indirizzi (diretto) con l'indirizzo di residenza del dichiarante.
            bool indirizzoCorrisponde = ConfrontaIndirizzi(indirizzoUtenza, numeroCivicoUtenza, dichiarante.IndirizzoResidenza, dichiarante.NumeroCivico, confrontoCivico);

            if (indirizzoCorrisponde)
            {
                // Aggiunge un messaggio di avvertimento se l'indirizzo della fornitura non coincide con quello INPS (se sono diversi).
                if (!ConfrontaIndirizzi(dichiarante.IndirizzoResidenza, dichiarante.NumeroCivico, indirizzoINPS, numeroCivicoINPS, confrontoCivico))
                {
                    message += "Attenzione: L'indirizzo della fornitura corrisponde a quello di residenza, ma non coincide con quello fornito dall'INPS.\n";
                }
                
                // 7. Indirizzo OK: Restituisce l'esito finale in base allo stato della fornitura (tramite funzione ausiliaria).
                return VerificaStatoFornitura(fornitura.stato, idFornituraTrovata, message, fornitura.id);
            }

            // 8. Se l'indirizzo non corrisponde, verifica se è disponibile una normalizzazione del toponimo.
            if (fornitura.idToponimo.HasValue)
            {
                var toponimo = context.Toponomi.FirstOrDefault(t => t.id == fornitura.idToponimo.Value && t.normalizzazione != null);

                if (toponimo != null)
                {
                    // Confronto con l'indirizzo normalizzato.
                    bool indirizzoToponimoCorrisponde = ConfrontaIndirizzi(toponimo.normalizzazione, numeroCivicoUtenza, dichiarante.IndirizzoResidenza, dichiarante.NumeroCivico, confrontoCivico);

                    if (indirizzoToponimoCorrisponde)
                    {
                        // Aggiunge un messaggio di avvertimento se l'indirizzo normalizzato non coincide con quello INPS.
                        if (!string.Equals(toponimo.normalizzazione, indirizzoINPS, StringComparison.OrdinalIgnoreCase))
                        {
                            message += "Attenzione: L'indirizzo normalizzato (" + toponimo.normalizzazione + ") non corrisponde a quello fornito dall'INPS.\n";
                        }
                        
                        // 9. Indirizzo normalizzato OK: Restituisce l'esito finale.
                        return VerificaStatoFornitura(fornitura.stato, idFornituraTrovata, message, fornitura.id);
                    }
                }
            }

            // 10. L'indirizzo (né l'originale né il normalizzato) non corrisponde. Esito "03".
            message += "Errore: L'indirizzo di ubicazione della fornitura non corrisponde a quello di residenza del dichiarante e/o quello fornito dall'INPS.";
            return ("03", idFornituraTrovata, message, fornitura.id);
        }


        // Metodo ausiliario per verificare lo stato
        private static (string esito, int? idFornitura, string? messaggio, int? idUtenza) VerificaStatoFornitura(int? stato, int? idFornitura, string? messaggio, int? idUtenza)
        {
            if (stato >= 1 && stato <= 3)
            {
                return ("01", idFornitura, messaggio, idUtenza);
            }
            else
            {
                return ("03", idFornitura, messaggio, idUtenza);
            }
        }

        // Metodo per confrontare due indirizzi (con o senza civico)
        private static bool ConfrontaIndirizzi(string? indirizzo1, string? civico1, string? indirizzo2, string? civico2, bool confrontoCivico)
        {
            if (confrontoCivico)
            {
                return string.Equals(indirizzo1, indirizzo2, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(civico1, civico2, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(indirizzo1, indirizzo2, StringComparison.OrdinalIgnoreCase);
        }

        public static string? FormattaIndirizzo(ApplicationDbContext context, string indirizzo_ubicazione, string codiceFiscale, int IdEnte)
        {
            // 1. Recupero il dichiarante
            var dichiarante = context.Dichiaranti.FirstOrDefault(s => s.CodiceFiscale == codiceFiscale && s.IdEnte == IdEnte);
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

        public static List<Ente> GetEnti(ApplicationDbContext _context, int? id)
        {
            if (id == null)
            {
                return new List<Ente>();
            }
            // Mi ricavo gli id degli enti che gestisce l'utente
            var idEnti = _context.UserEnti
                .Where(s => s.idUser == id)
                .Select(s => s.idEnte) // prendo solo l'idEnte
                .ToList();

            // Se non ha enti associati, ritorno una lista vuota
            if (!idEnti.Any())
            {
                return new List<Ente>();
            }

            // Recupero direttamente gli enti dalla tabella Enti
            var enti = _context.Enti
                .Where(e => idEnti.Contains(e.id))
                .ToList();

            return enti;
        }

        public static int CalcolaEta(DateTime dataNascita)
        {
            var oggi = DateTime.Today;
            int eta = oggi.Year - dataNascita.Year;

            // Se il compleanno non è ancora passato quest'anno, tolgo 1
            if (dataNascita.Date > oggi.AddYears(-eta))
            {
                eta--;
            }

            return eta;
        }

        public static string? getNominativoDichiarante(ApplicationDbContext _context, int? id)
        {
            if (id == null || id == 0)
            {
                return null;
            }
            // cerco il dichiarante
            var dichiarante = _context.Dichiaranti.FirstOrDefault(s => s.id == id);
            if (dichiarante == null)
            {
                return null;
            }
            return dichiarante.Cognome + " " + dichiarante.Nome;
        }

    }
}
