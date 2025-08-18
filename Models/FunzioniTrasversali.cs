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


        public static (string esito, int? idFornitura) VerificaEsistenzaFornitura(string codiceFiscale, int selectedEnteId, ApplicationDbContext context, string indirizzoResidenza, string numeroCivico, string cognome, string? nome, DateTime? data_nascita)
        {
            // Recupera le forniture associate al codice fiscale e all'ente selezionato
            var forniture = context.UtenzeIdriche
                .Where(s => (s.codiceFiscale == codiceFiscale || (s.cognome == cognome && s.nome == nome && s.DataNascita == data_nascita)) && s.IdEnte == selectedEnteId)
                .ToList();

            if (forniture.Count != 1)
            {
                // Nessuna fornitura o più forniture trovate
                return ("04", null);
            }

            var fornitura = forniture[0];
            int? idFornituraTrovata = int.TryParse(fornitura.idAcquedotto, out int idF) ? idF : null;

            // Verifica il tipo di utenza
            if (!string.Equals(fornitura.tipoUtenza, "UTENZA DOMESTICA", StringComparison.OrdinalIgnoreCase))
            {
                return ("03", null);
            }

            // Recupera i dati dell'utenza
            string? indirizzoUtenza = fornitura.indirizzoUbicazione;
            string? numeroCivicoUtenza = fornitura.numeroCivico;
            int? idToponimo = fornitura.idToponimo;

            bool indirizzoCorrisponde = string.Equals(indirizzoUtenza, indirizzoResidenza, StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(numeroCivicoUtenza, numeroCivico, StringComparison.OrdinalIgnoreCase);

            // Se indirizzo e numero civico coincidono
            if (indirizzoCorrisponde)
            {
                return VerificaStatoFornitura(fornitura.stato, idFornituraTrovata);
            }

            // Se non coincidono, verifica se è disponibile una normalizzazione del toponimo
            if (idToponimo.HasValue)
            {
                var toponimo = context.Toponomi
                    .FirstOrDefault(t => t.id == idToponimo.Value && t.normalizzazione != null);

                if (toponimo != null)
                {
                    indirizzoUtenza = toponimo.normalizzazione;

                    bool indirizzoToponimoCorrisponde = string.Equals(indirizzoUtenza, indirizzoResidenza, StringComparison.OrdinalIgnoreCase) &&
                                                        string.Equals(numeroCivicoUtenza, numeroCivico, StringComparison.OrdinalIgnoreCase);

                    if (indirizzoToponimoCorrisponde)
                    {
                        return VerificaStatoFornitura(fornitura.stato, idFornituraTrovata);
                    }
                }
            }

            // L’indirizzo non corrisponde
            return ("03", idFornituraTrovata);
        }

        // Metodo ausiliario per verificare lo stato
        private static (string esito, int? idFornitura) VerificaStatoFornitura(int? stato, int? idFornitura)
        {
            if (stato >= 1 && stato <= 3)
            {
                return ("01", idFornitura);
            }
            else
            {
                return ("03", idFornitura);
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
    }
}
