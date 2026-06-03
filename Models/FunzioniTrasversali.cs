using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        private static readonly Dictionary<string, string> Ordinali = new()
        {
            ["I"] = "PRIMO", ["II"] = "SECONDO", ["III"] = "TERZO", ["IV"] = "QUARTO", ["V"] = "QUINTO",
            ["VI"] = "SESTO", ["VII"] = "SETTIMO", ["VIII"] = "OTTAVO", ["IX"] = "NONO", ["X"] = "DECIMO",
            ["XI"] = "UNDICESIMO", ["XII"] = "DODICESIMO", ["XIII"] = "TREDICESIMO", ["XIV"] = "QUATTORDICESIMO", ["XV"] = "QUINDICESIMO",
            ["XVI"] = "SEDICESIMO", ["XVII"] = "DICIASSETTESIMO", ["XVIII"] = "DICIOTTESIMO", ["XIX"] = "DICIANNOVESIMO", ["XX"] = "VENTESIMO",
            ["1"] = "PRIMO", ["2"] = "SECONDO", ["3"] = "TERZO", ["4"] = "QUARTO", ["5"] = "QUINTO",
            ["6"] = "SESTO", ["7"] = "SETTIMO", ["8"] = "OTTAVO", ["9"] = "NONO", ["10"] = "DECIMO",
            ["11"] = "UNDICESIMO", ["12"] = "DODICESIMO", ["13"] = "TREDICESIMO", ["14"] = "QUATTORDICESIMO", ["15"] = "QUINDICESIMO",
            ["16"] = "SEDICESIMO", ["17"] = "DICIASSETTESIMO", ["18"] = "DICIOTTESIMO", ["19"] = "DICIANNOVESIMO", ["20"] = "VENTESIMO",
            ["PRIMA"] = "PRIMO", ["SECONDA"] = "SECONDO", ["TERZA"] = "TERZO", ["QUARTA"] = "QUARTO", ["QUINTA"] = "QUINTO",
            ["SESTA"] = "SESTO", ["SETTIMA"] = "SETTIMO", ["OTTAVA"] = "OTTAVO", ["NONA"] = "NONO", ["DECIMA"] = "DECIMO"
        };

        private static readonly HashSet<string> TipiToponimoOrdinali = new()
        {
            "VICOLO", "TRAVERSA", "CONTRADA", "CORSO", "VICO"
        };

        public static string rimuoviVirgolette(string? stringa)
        {
            string formatedString = (stringa ?? string.Empty).Trim();
            if (formatedString.Contains("\""))
            {
                // Rimuove gli spazi e i caratteri speciali come / e -
                formatedString = formatedString.Replace("\"", "");
            }
            return formatedString;
        }

        public static string FormattaNumeroCivico(string? stringa)
        {
            return NormalizeNumeroCivico(stringa);
        }

        public static (string Toponimo, string? NumeroCivico, string? CivicoEstratto) ExtractToponimoAndCivico(string? indirizzoCompleto, string? numeroCivicoSeparato = null)
        {
            string indirizzo = RemoveAccents(rimuoviVirgolette(indirizzoCompleto).ToUpperInvariant());
            indirizzo = Regex.Replace(indirizzo, @"\s+", " ").Trim();
            string civicoSeparato = NormalizeNumeroCivico(numeroCivicoSeparato);

            if (string.IsNullOrWhiteSpace(indirizzo))
                return (string.Empty, string.IsNullOrWhiteSpace(civicoSeparato) ? null : civicoSeparato, null);

            string toponimo = indirizzo;
            string? civicoEstratto = null;

            var sncMatch = Regex.Match(toponimo, @"\s+(S\s*\.?\s*N\s*\.?\s*C\s*\.?|S\s*/\s*N|SNC|SENZA\s+NUMERO(?:\s+CIVICO)?)\s*$", RegexOptions.IgnoreCase);
            if (sncMatch.Success)
            {
                civicoEstratto = "SNC";
                toponimo = toponimo[..sncMatch.Index];
            }
            else
            {
                var civicoMatch = Regex.Match(toponimo, @"\s+(?:N\s*\.?\s*|NUMERO\s+)?(\d+)\s*(?:[/\-\s]\s*([A-Z]))?\s*$", RegexOptions.IgnoreCase);
                if (civicoMatch.Success)
                {
                    string toponimoSenzaCivico = toponimo[..civicoMatch.Index];
                    if (!Regex.IsMatch(NormalizeToponimoSenzaEstrazione(toponimoSenzaCivico), @"\bSTRADA\s+(PROVINCIALE|STATALE)$"))
                    {
                        civicoEstratto = NormalizeNumeroCivico(civicoMatch.Groups[1].Value + civicoMatch.Groups[2].Value);
                        toponimo = toponimoSenzaCivico;
                    }
                }
            }

            string civicoFinale = string.IsNullOrWhiteSpace(civicoSeparato)
                ? (civicoEstratto ?? string.Empty)
                : civicoSeparato;

            toponimo = Regex.Replace(toponimo, @"[^A-Z0-9]+", " ");
            toponimo = Regex.Replace(toponimo, @"\s+", " ").Trim();

            return (toponimo, string.IsNullOrWhiteSpace(civicoFinale) ? null : civicoFinale, civicoEstratto);
        }

        private static string NormalizeToponimoSenzaEstrazione(string? input)
        {
            string value = RemoveAccents(rimuoviVirgolette(input).ToUpperInvariant());
            value = Regex.Replace(value, @"\bS\s*\.?\s*S\s*\.?\b", " STRADA STATALE ");
            value = Regex.Replace(value, @"\bSP\b", " STRADA PROVINCIALE ");
            value = Regex.Replace(value, @"[^A-Z0-9]+", " ");
            return Regex.Replace(value, @"\s+", " ").Trim();
        }

        public static string NormalizeToponimo(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var indirizzoPulito = ExtractToponimoAndCivico(input);
            string value = RemoveAccents(rimuoviVirgolette(indirizzoPulito.Toponimo).ToUpperInvariant());

            value = Regex.Replace(value, @"\bS\s*\.?\s*S\s*\.?\b", " STRADA STATALE ");
            value = Regex.Replace(value, @"\bP\s*\.?\s*ZZA\b", " PIAZZA ");
            value = Regex.Replace(value, @"\bPZA\b", " PIAZZA ");
            value = Regex.Replace(value, @"\bC\s*\.?\s*SO\b", " CORSO ");
            value = Regex.Replace(value, @"\bCSO\b", " CORSO ");
            value = Regex.Replace(value, @"\bL\s*\.?\s*GO\b", " LARGO ");
            value = Regex.Replace(value, @"\bLGO\b", " LARGO ");
            value = Regex.Replace(value, @"\bV\s*\.?\s*LE\b", " VIALE ");
            value = Regex.Replace(value, @"\bVLE\b", " VIALE ");
            value = Regex.Replace(value, @"\bVIC\b\s*\.?", " VICOLO ");
            value = Regex.Replace(value, @"\bVICO\b", " VICOLO ");
            value = Regex.Replace(value, @"\bVCL\b", " VICOLO ");
            value = Regex.Replace(value, @"\bCONTR\b\s*\.?", " CONTRADA ");
            value = Regex.Replace(value, @"\bCDA\b", " CONTRADA ");
            value = Regex.Replace(value, @"\bS\s*\.?\s*N\s*\.?\s*C\s*\.?\b", " SENZA NUMERO CIVICO ");
            value = Regex.Replace(value, @"\bSNC\b", " SENZA NUMERO CIVICO ");
            value = Regex.Replace(value, @"\bV\s*\.", " VIA ");
            value = Regex.Replace(value, @"\bVIA\s*\.", " VIA ");
            value = Regex.Replace(value, @"\bSP\s+(\d+)\b", " STRADA PROVINCIALE $1 ");
            value = Regex.Replace(value, @"(VICOLO|VICO|TRAVERSA|CONTRADA|CORSO)(I{1,3}|IV|V|VI{0,3}|IX|X|XI{0,3}|XIV|XV|XVI{0,3}|XIX|XX|\d{1,2})\b", "$1 $2");
            value = Regex.Replace(value, @"(\d+)\s*[°º]", "$1");
            value = Regex.Replace(value, @"[^A-Z0-9]+", " ");
            value = Regex.Replace(value, @"\s+", " ").Trim();

            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            for (int i = 0; i < tokens.Count; i++)
            {
                if (Ordinali.TryGetValue(tokens[i], out string? ordinale) && DeveConvertireOrdinale(tokens, i))
                    tokens[i] = ordinale;
            }

            return string.Join(" ", tokens);
        }

        public static string NormalizeIndirizzoCompleto(string? indirizzo, string? numeroCivico = null, string? scala = null, string? piano = null, string? interno = null)
        {
            var indirizzoSeparato = ExtractToponimoAndCivico(indirizzo, numeroCivico);
            string indirizzoPulito = indirizzoSeparato.Toponimo;
            string civico = indirizzoSeparato.NumeroCivico ?? string.Empty;

            string toponimo = NormalizeToponimo(indirizzoPulito);
            string scalaNorm = NormalizeEtichetta("SCALA", scala, "SC");
            string pianoNorm = NormalizeEtichetta("PIANO", piano, "P");
            string internoNorm = NormalizeEtichetta("INTERNO", interno, "INT");

            return string.Join(" ", new[] { toponimo, civico, scalaNorm, pianoNorm, internoNorm }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        public static bool AreToponimiCompatibili(string? a, string? b)
        {
            string normalizedA = NormalizeToponimo(a);
            string normalizedB = NormalizeToponimo(b);

            if (string.IsNullOrWhiteSpace(normalizedA) || string.IsNullOrWhiteSpace(normalizedB))
                return false;

            if (string.Equals(normalizedA, normalizedB, StringComparison.OrdinalIgnoreCase))
                return true;

            var tokensA = normalizedA.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var tokensB = normalizedB.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokensA.Length != tokensB.Length || tokensA.Length < 3)
                return false;

            if (!string.Equals(tokensA[0], tokensB[0], StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(tokensA[^1], tokensB[^1], StringComparison.OrdinalIgnoreCase))
                return false;

            bool usataIniziale = false;
            for (int i = 1; i < tokensA.Length - 1; i++)
            {
                if (string.Equals(tokensA[i], tokensB[i], StringComparison.OrdinalIgnoreCase))
                    continue;

                bool inizialeCompatibile =
                    tokensA[i].Length == 1 && tokensB[i].Length > 1 && tokensB[i].StartsWith(tokensA[i], StringComparison.OrdinalIgnoreCase) ||
                    tokensB[i].Length == 1 && tokensA[i].Length > 1 && tokensA[i].StartsWith(tokensB[i], StringComparison.OrdinalIgnoreCase);

                if (!inizialeCompatibile)
                    return false;

                usataIniziale = true;
            }

            return usataIniziale;
        }

        private static string NormalizeNumeroCivico(string? input)
        {
            string value = RemoveAccents(rimuoviVirgolette(input).ToUpperInvariant());
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = Regex.Replace(value, @"\b(S\s*\.?\s*N\s*\.?\s*C\s*\.?|S\s*/\s*N|SN|SENZA\s+NUMERO(?:\s+CIVICO)?)\b", "SNC");
            value = Regex.Replace(value, @"[^A-Z0-9]+", " ").Trim();

            if (value == "0" || value == "SNC")
                return "SNC";

            var match = Regex.Match(value, @"^(\d+)\s*([A-Z])?$");
            if (match.Success)
                return match.Groups[1].Value + match.Groups[2].Value;

            match = Regex.Match(value, @"^(\d+)");
            return match.Success ? match.Groups[1].Value : value.Replace(" ", "");
        }

        private static string NormalizeEtichetta(string label, string? value, string abbreviazione)
        {
            string normalized = RemoveAccents(rimuoviVirgolette(value).ToUpperInvariant());
            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            normalized = Regex.Replace(normalized, @"[^A-Z0-9]+", " ").Trim();
            normalized = Regex.Replace(normalized, $@"^({label}|{abbreviazione})\s*", "", RegexOptions.IgnoreCase).Trim();
            return string.IsNullOrWhiteSpace(normalized) ? string.Empty : $"{label} {normalized}";
        }

        private static string RemoveAccents(string input)
        {
            string normalized = input.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static bool DeveConvertireOrdinale(List<string> tokens, int index)
        {
            if (index == 0)
                return false;

            string token = tokens[index];
            if (!Ordinali.ContainsKey(token))
                return false;

            string tipo = tokens[0];
            if (tipo == "VIA" && index == 1 && index + 1 < tokens.Count && tokens[index + 1] == "MAGGIO")
                return false;

            if (TipiToponimoOrdinali.Contains(tokens[index - 1]) || TipiToponimoOrdinali.Contains(tipo))
                return true;

            return token.Length > 1;
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
                return null; // Restituisce null se la stringa Ã¨ vuota/nulla
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
            Console.WriteLine($"Attenzione: Impossibile convertire la data '{dataStringa}' nel formato atteso. VerrÃ  salvato un valore nullo.");
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
            Console.WriteLine($"Attenzione: Impossibile convertire la data '{dataStringa}' nel formato atteso. VerrÃ  restituito il valore di default.");
            return defaultValue;
        }

        /*
            NOME FUNZIONE: VerificaEsistenzaFornitura()

            SCOPO:
                Questa funzione ha lâ€™obiettivo di verificare se un soggetto ha diritto a ricevere il bonus idrico.
                In particolare, controlla se il dichiarante Ã¨ associato ad una fornitura idrica valida, ovvero:
                    - situata presso lo stesso indirizzo comunicato dallâ€™INPS;
                    - classificata come utenza domestica.
                Inoltre, se richiesto, vengono effettuati controlli anche sul numero civico.

            PARAMETRI IN INGRESSO:
                - dichiarante: dati anagrafici del soggetto da verificare;
                - selectedEnteId: identificativo dellâ€™ente su cui eseguire lâ€™operazione;
                - context: connessione al database per lâ€™esecuzione delle query;
                - indirizzoINPS: indirizzo fornito dal file CSV dellâ€™INPS, utilizzato per il confronto;
                - numeroCivico: numero civico fornito dal file CSV dellâ€™INPS. Viene confrontato solo se confrontaCivico = true;
                - confrontaCivico: valore booleano che stabilisce se considerare o meno il campo numero civico nel confronto.

            PARAMETRI IN USCITA:
                - Esito: rappresenta lâ€™esito della verifica, con valori da "01" a "04":
                    - "01": fornitura trovata e conforme a tutti i requisiti;
                    - "02": nessuna fornitura trovata, ma "Presenza POD" = "SI". 
                            (Nota: questo esito non viene restituito direttamente da questa funzione, 
                            poichÃ© richiede ulteriori verifiche preliminari);
                    - "03": fornitura trovata, ma non conforme ai requisiti (es. indirizzo diverso da quello INPS, utenza non domestica);
                    - "04": nessuna fornitura trovata e "Presenza POD" = "NO".
                - ID Fornitura: identificativo della fornitura idrica trovata (puÃ² essere null se nessuna fornitura Ã¨ stata individuata);
                - Messaggio: eventuale messaggio descrittivo o di avvertimento che spiega il motivo dellâ€™esito negativo
                            (salvato come nota nel database);
                - ID Utenza: identificativo dellâ€™utenza salvata nel database, distinto dallâ€™ID Fornitura.

            NOTE:
                - Il campo "Presenza POD" indica se Ã¨ presente un contatore attivo.

            ARTICOLAZIONE DELLA FUNZIONE:
                1. Recupero delle forniture associate al soggetto richiedente nellâ€™ente selezionato.
                2. Verifica del numero di forniture trovate:
                    - Se nessuna fornitura Ã¨ presente â†’ restituisco esito "04" e messaggio "Nessuna fornitura trovata per il dichiarante".
                3. Se sono presenti piÃ¹ forniture â†’ controllo la corrispondenza con lâ€™indirizzo comunicato dallâ€™INPS.
                4. Verifica che la fornitura sia di tipo domestico. 
                    - In caso contrario â†’ esito "03" e messaggio "Attenzione: la fornitura trovata non Ã¨ di tipo 'UTENZA DOMESTICA'".
                5. Confronto lâ€™indirizzo della fornitura con quello fornito dallâ€™INPS ed eventualmente anche il numero civico.
                6. Se lâ€™indirizzo non coincide, verifico la corrispondenza tramite la normalizzazione del toponimo.
                7. Se lâ€™indirizzo o il toponimo coincidono â†’ restituisco esito "01", ID fornitura, ID utenza ed eventuali messaggi.
                8. Se nÃ© lâ€™indirizzo nÃ© il toponimo coincidono â†’ restituisco esito "03", messaggio di errore, ID fornitura e ID utenza.
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
                // 2. Se piÃ¹ forniture, cerca quella all'indirizzo di residenza del dichiarante per disambiguare.
                foreach (var utenza in forniture)
                {
                    if (ConfrontaIndirizzi(utenza.indirizzoUbicazione, utenza.numeroCivico, dichiarante.IndirizzoResidenza, dichiarante.NumeroCivico, true))
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
                return ("03", idFornituraTrovata, "Attenzione: La fornitura trovata non Ã¨ di tipo 'UTENZA DOMESTICA'.", fornitura.id);
            }

            string? indirizzoUtenza = fornitura.indirizzoUbicazione;
            string? numeroCivicoUtenza = fornitura.numeroCivico;
            string message = "";

            // 6. Confronto Indirizzi (diretto) con l'indirizzo di residenza del dichiarante.
            bool indirizzoCorrisponde = ConfrontaIndirizzi(indirizzoUtenza, numeroCivicoUtenza, dichiarante.IndirizzoResidenza, dichiarante.NumeroCivico, confrontoCivico);

            if (indirizzoCorrisponde)
            {
                if (!string.Equals(indirizzoUtenza, dichiarante.IndirizzoResidenza, StringComparison.OrdinalIgnoreCase))
                {
                    message += $"Fornitura trovata tramite indirizzo normalizzato: utenza '{NormalizeIndirizzoCompleto(indirizzoUtenza, numeroCivicoUtenza)}' - residenza '{NormalizeIndirizzoCompleto(dichiarante.IndirizzoResidenza, dichiarante.NumeroCivico)}'.\n";
                }

                // Aggiunge un messaggio di avvertimento se l'indirizzo della fornitura non coincide con quello INPS (se sono diversi).
                if (!ConfrontaIndirizzi(dichiarante.IndirizzoResidenza, dichiarante.NumeroCivico, indirizzoINPS, numeroCivicoINPS, confrontoCivico))
                {
                    message += "Attenzione: L'indirizzo della fornitura corrisponde a quello di residenza, ma non coincide con quello fornito dall'INPS.\n";
                }
                
                // 7. Indirizzo OK: Restituisce l'esito finale in base allo stato della fornitura (tramite funzione ausiliaria).
                return VerificaStatoFornitura(fornitura.stato, idFornituraTrovata, message, fornitura.id);
            }

            // 8. Se l'indirizzo non corrisponde, verifica se Ã¨ disponibile una normalizzazione del toponimo.
            if (fornitura.idToponimo.HasValue)
            {
                var toponimo = context.Toponomi.FirstOrDefault(t => t.id == fornitura.idToponimo.Value && t.normalizzazione != null);

                if (toponimo != null)
                {
                    // Confronto con l'indirizzo normalizzato.
                    bool indirizzoToponimoCorrisponde = ConfrontaIndirizzi(toponimo.normalizzazione, numeroCivicoUtenza, dichiarante.IndirizzoResidenza, dichiarante.NumeroCivico, confrontoCivico);

                    if (indirizzoToponimoCorrisponde)
                    {
                        message += $"Fornitura trovata tramite indirizzo normalizzato: toponimo '{NormalizeIndirizzoCompleto(toponimo.normalizzazione, numeroCivicoUtenza)}' - residenza '{NormalizeIndirizzoCompleto(dichiarante.IndirizzoResidenza, dichiarante.NumeroCivico)}'.\n";

                        // Aggiunge un messaggio di avvertimento se l'indirizzo normalizzato non coincide con quello INPS.
                        if (!ConfrontaIndirizzi(toponimo.normalizzazione, numeroCivicoUtenza, indirizzoINPS, numeroCivicoINPS, confrontoCivico))
                        {
                            message += "Attenzione: L'indirizzo normalizzato (" + toponimo.normalizzazione + ") non corrisponde a quello fornito dall'INPS.\n";
                        }
                        
                        // 9. Indirizzo normalizzato OK: Restituisce l'esito finale.
                        return VerificaStatoFornitura(fornitura.stato, idFornituraTrovata, message, fornitura.id);
                    }
                }
            }

            // 10. L'indirizzo (nÃ© l'originale nÃ© il normalizzato) non corrisponde. Esito "03".
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
            string indirizzoNormalizzato1 = NormalizeToponimo(indirizzo1);
            string indirizzoNormalizzato2 = NormalizeToponimo(indirizzo2);

            if (string.IsNullOrWhiteSpace(indirizzoNormalizzato1) || string.IsNullOrWhiteSpace(indirizzoNormalizzato2))
                return false;

            if (confrontoCivico)
            {
                var separato1 = ExtractToponimoAndCivico(indirizzo1, civico1);
                var separato2 = ExtractToponimoAndCivico(indirizzo2, civico2);

                if (!string.Equals(separato1.NumeroCivico, separato2.NumeroCivico, StringComparison.OrdinalIgnoreCase))
                    return false;

                return string.Equals(NormalizeToponimo(separato1.Toponimo), NormalizeToponimo(separato2.Toponimo), StringComparison.OrdinalIgnoreCase) ||
                    AreToponimiCompatibili(separato1.Toponimo, separato2.Toponimo);
            }

            return string.Equals(indirizzoNormalizzato1, indirizzoNormalizzato2, StringComparison.OrdinalIgnoreCase) ||
                AreToponimiCompatibili(indirizzo1, indirizzo2);
        }

        public static string? FormattaIndirizzo(ApplicationDbContext context, string indirizzo_ubicazione, string codiceFiscale, int IdEnte)
        {
            // 1. Recupero il dichiarante
            var dichiarante = context.Dichiaranti.FirstOrDefault(s => s.CodiceFiscale == codiceFiscale && s.IdEnte == IdEnte);
            if (dichiarante == null) { return null; }

            var indirizzoResidenza = rimuoviVirgolette(dichiarante.IndirizzoResidenza).Trim();
            var indirizzoUbicazione = rimuoviVirgolette(indirizzo_ubicazione).Trim();
            var indirizzoResidenzaNorm = NormalizeToponimo(indirizzoResidenza);
            var indirizzoUbicazioneNorm = NormalizeToponimo(indirizzoUbicazione);

            // 2. Confronto diretto ignorando le maiuscole
            if (string.Equals(indirizzoUbicazioneNorm, indirizzoResidenzaNorm, StringComparison.OrdinalIgnoreCase))
            {
                return indirizzoResidenzaNorm;
            }

            return indirizzoUbicazioneNorm;
        }

        // Funzione che analizza un indirizzo per ricavare il suo tipo_toponimo e l'intestazione

        public static (string? tipoToponimo, string? intestazione) AnalizzaIndirizzoPerToponimo(string indirizzo)
        {
            if (string.IsNullOrWhiteSpace(indirizzo))
            {
                return (null, null);
            }

            var indirizzoNormalizzato = NormalizeToponimo(indirizzo);
            var partiIndirizzo = indirizzoNormalizzato.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (partiIndirizzo.Length < 2)
            {
                return (null, null);
            }

            var possibileTipo = partiIndirizzo[0];
            var intestazione = partiIndirizzo[1];

            if (!Toponimo.IsTipoToponimoValido(possibileTipo))
            {
                return (null, null);
            }

            return (possibileTipo, intestazione);
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

            // Se il compleanno non Ã¨ ancora passato quest'anno, tolgo 1
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

        // Funzione che restituisce lo stato dell'utenza idrica come stringa
        public static string getStatoStringUtenza(int? stato)
        {
            //Valori campo stato (1=Iscrivendo;2=Iscritto;3=Iscrivendo/Cancellando;4=Cancellando;5=Cancellato)
            return stato switch
            {
                1 => "Iscrivendo",
                2 => "Iscritto",
                3 => "Iscrivendo/Cancellando",
                4 => "Cancellando",
                5 => "Cancellato",
                _ => "Sconosciuto",
            };
        }

        public static string MaiuscoleIniziali(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var parole = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parole.Length; i++)
            {
                parole[i] = char.ToUpper(parole[i][0]) + parole[i].Substring(1).ToLower();
            }

            return string.Join(" ", parole);
        }

    }
}
