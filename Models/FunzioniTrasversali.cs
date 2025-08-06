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


        public static (string esito, int? idFornitura) verificaEsisistenzaFornitura(string codiceFiscale, int selectedEnteId, ApplicationDbContext context, string IndirizzoResidenza, string NumeroCivico)
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
