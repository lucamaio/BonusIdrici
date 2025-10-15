using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    /*
        Questa classe rappresenta un report generato durante l'elaborazione dei bonus idrici.
        Contiene informazioni dettagliate sul risultato dell'elaborazione per ogni richiesta di bonus.

        CAMPI OBBLIGATORI:
            - idAto
            - codiceBonus
            - esitoStr
            - esito
            - codiceFiscaleRichiedente
            - nomeDichiarante
            - cognomeDichiarante
            - annoValidita
            - indirizzoAbitazione
            - istat
            - capAbitazione
            - presenzaPod
            - serie
            - dataInizioValidita

        CAMPI FACOLTATIVI:
            - idFornitura
            - codiceFiscaleUtenzaTrovata
            - idUtenza
            - numeroComponenti
            - idDichiarante
            - numeroCivico
            - provinciaAbitazione
            - note
            - incongruenze
            - mc
        Meta Dati:
            - idReport
            - Data Aggiornamento
    */
    public class Domanda
    {
        // Chiave primaria
        [Key]
        public int id { get; set; }

        // Campi obbligatori ricevuti dal file csv di input

        [Required]
        public required string idAto { get; set; }

        [Required]
        public required string codiceBonus { get; set; }

        [Required]
        public required string esitoStr { get; set; }  // esito "Si" o "No"

        [Required]
        public required string esito { get; set; }  // Indica se il bonus è stato concesso o negato

        public int? idFornitura { get; set; }       // Identificativo della fornitura, se disponibile

        [Required]
        public required string codiceFiscaleRichiedente { get; set; } // Codice fiscale del richiedente del bonus (da file csv di input)

        public string? codiceFiscaleUtenzaTrovata { get; set; } // Codice fiscale associato all'utenza trovata, se disponibile

        public int? idUtenza { get; set; }   // Identificativo dell'utenza idrica trovata, se disponibile

        public int? numeroComponenti { get; set; }  // Numero di componenti del nucleo familiare


        // Dati anagrafici del dichiarante (da file csv di input)

        [Required]
        public required string nomeDichiarante { get; set; }

        [Required]
        public required string cognomeDichiarante { get; set; }

        public int? idDichiarante { get; set; }

        [Required]
        public required string indirizzoAbitazione { get; set; }

        public string? numeroCivico { get; set; }

        [Required]
        public required string istat { get; set; }

        [Required]
        public required string capAbitazione { get; set; }

        public string? provinciaAbitazione { get; set; }

        // Indica se è presente un POD associato all'utenza idrica (tipicamente "Si" o "No")
        [Required]
        public required string presenzaPod { get; set; }

        // Campi di controllo se sono presenti incongruenze nei dati che devono essere verificate manualmente

        public string? note { get; set; }       // Indica eventuali note sull'elaborazione

        public bool? incongruenze { get; set; } // Indica se sono state rilevate incongruenze nei dati

        
        // Campo facoltativo per memorizzare i metri cubi (mc) associati al bonus, se disponibile
        // Tale campo viene generato solo se il bonus è stato concesso. Viene calcolato secondo la funzione specifica (calcolaMC della classe CSVReader).
        public int? mc { get; set; }

        // Periodo di validità dell'eventuale bonus concesso (da file csv di input)

        [Required]
        public required string annoValidita { get; set; }

        [Required]
        public required DateTime dataInizioValidita { get; set; }

        [Required]
        public required DateTime dataFineValidita { get; set; }

        // Metadati

        [Required]
        public required int idReport {get; set;}
        
        public DateTime? DataAggiornamento { get; set; }

        public override string ToString()
        {
            return $"Report: id={id}, codiceBonus={codiceBonus}, esitoStr={esitoStr}, esito={esito}, idFornitura={idFornitura}, codiceFiscale={codiceFiscaleRichiedente}, " +
                $"numeroComponenti ={numeroComponenti}, nomeDichiarante={nomeDichiarante}, cognomeDichiarante={cognomeDichiarante}, annoValidita={annoValidita}, " +
                $"indirizzoAbitazione ={indirizzoAbitazione}, numeroCivico={numeroCivico}, istat={istat}, capAbitazione={capAbitazione}, provinciaAbitazione={provinciaAbitazione}, " +
                $"presenzaPod ={presenzaPod}, dataInizioValidita={dataInizioValidita}, dataFineValidita={dataFineValidita}, Data Aggiornamento: {DataAggiornamento}";
        }
    }
}