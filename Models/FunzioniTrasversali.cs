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


namespace BonusIdrici2.Models
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


        


        public static (string esito, int? idFornitura, string? messaggio) VerificaEsistenzaFornitura(string codiceFiscale, int selectedEnteId, ApplicationDbContext context, Dichiarante dichiarante, string indirizzoINPS, string numeroCivicoINPS)
        {
            // Recupera le forniture associate al codice fiscale e all'ente selezionato
            var forniture = context.UtenzeIdriche
                .Where(s => ((s.codiceFiscale == dichiarante.CodiceFiscale || (s.cognome == dichiarante.Cognome && s.nome == dichiarante.Nome && s.DataNascita == dichiarante.DataNascita)) && (s.stato != 5 && s.stato != 4)) && s.IdEnte == selectedEnteId)
                .ToList();

            if(forniture.Count == 0)
            {
                // Nessuna fornitura trovata
                return ("04", null, "Nessuna fornitura trovata per il dichiarante.");
            }else if(forniture.Count > 1){
                // Più forniture trovate
                return ("04", null, "Attenzione: piu' di una fornitura trovata per il dichiarante.");

            }

            var fornitura = forniture[0];
            int? idFornituraTrovata = int.TryParse(fornitura.idAcquedotto, out int idF) ? idF : null;

            // Verifica il tipo di utenza
            if (!string.Equals(fornitura.tipoUtenza, "UTENZA DOMESTICA", StringComparison.OrdinalIgnoreCase))
            {
                return ("03", null, "Attenzione: La fornitura trovata non è di tipo 'UTENZA DOMESTICA'.\n");
            }

            // Recupera i dati dell'utenza
            string? indirizzoUtenza = fornitura.indirizzoUbicazione;
            string? numeroCivicoUtenza = fornitura.numeroCivico;
            int? idToponimo = fornitura.idToponimo;
            string? message = null;

            bool indirizzoCorrisponde = string.Equals(indirizzoUtenza, dichiarante.IndirizzoResidenza, StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(numeroCivicoUtenza, dichiarante.NumeroCivico, StringComparison.OrdinalIgnoreCase);

            // Se indirizzo e numero civico coincidono
            if (indirizzoCorrisponde)
            {
                if(dichiarante.IndirizzoResidenza != indirizzoINPS || dichiarante.NumeroCivico != numeroCivicoINPS){
                    message = message + "Attenzione: L'indirizzo di ubicazione o il numero civico della fornitura corrisponde esattamente all'indirizzo di residenza del dichiarante, ma non corrisponde a quello fornito dal INPS. \n";
                }

                return VerificaStatoFornitura(fornitura.stato, idFornituraTrovata, message);
            }else{
                if(dichiarante.IndirizzoResidenza == indirizzoUtenza && ( dichiarante.IndirizzoResidenza != indirizzoINPS || dichiarante.NumeroCivico != numeroCivicoINPS)){
                    message = message +"Attenzione: L'indirizzo di ubicazione o il numero civico non corrisponde esattamente all'indirizzo di residenza del dichiarante, fornito dal INPS.\n";
                }
            }

            // Se non coincidono, verifica se è disponibile una normalizzazione del toponimo
            if (idToponimo != null)
            {
                var toponimo = context.Toponomi.FirstOrDefault(t => t.id == idToponimo && t.normalizzazione != null);

                if (toponimo != null)
                {
                    indirizzoUtenza = toponimo.normalizzazione;

                    bool indirizzoToponimoCorrisponde = string.Equals(indirizzoUtenza, dichiarante.IndirizzoResidenza, StringComparison.OrdinalIgnoreCase) && string.Equals(numeroCivicoUtenza, dichiarante.NumeroCivico, StringComparison.OrdinalIgnoreCase);

                    if (indirizzoToponimoCorrisponde)
                    {
                        if(toponimo.normalizzazione != indirizzoINPS){
                            message = message + "Attenzione: L'indirizzo di ubicazione del toponimo non corrisponde a quello fornito dal INPS.\n";
                        }
                        return VerificaStatoFornitura(fornitura.stato, idFornituraTrovata, message);
                    }
                }
            }

            // L’indirizzo non corrisponde
            return ("03", idFornituraTrovata, message);
        }

        // Metodo ausiliario per verificare lo stato
        private static (string esito, int? idFornitura, string? messaggio) VerificaStatoFornitura(int? stato, int? idFornitura, string? messaggio)
        {
            if (stato >= 1 && stato <= 3)
            {
                return ("01", idFornitura, messaggio);
            }
            else
            {
                return ("03", idFornitura, messaggio);
            }
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

        public static List<Ente> GetEnti(ApplicationDbContext _context, int id)
        {
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

    }
}
