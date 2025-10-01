using System;
using System.Net;
using System.Net.Mail;

namespace Models
{
    /*
        Questa classe rappresenta una email.
        Viene utilizzata per inviare email tramite SMTP.

        PROPRIETA':
        - Destinatario: indirizzo email del destinatario.
        - Oggetto: oggetto dell'email.
        - Corpo: corpo dell'email.

        COSTRUTTORE:
        - Email(): costruttore di default che inizializza le proprietà a stringhe vuote.
        - Email(string destinatario, string oggetto, string corpo): costruttore che accetta i valori delle proprietà.

        METODI:
        - EmailValida(): verifica se l'indirizzo email del destinatario è valido.
        - Invia(): invia l'email utilizzando le impostazioni SMTP predefinite.
        - ToString(): override del metodo ToString per una rappresentazione leggibile dell'email.

    */

    public class Email
    {
        // Propietà
        public string Destinatario { get; set; }
        public string Oggetto { get; set; }
        public string Corpo { get; set; }

        // Oggetto MailMessage per l'invio    
        private MailMessage mail = new MailMessage();

        // Configurazione SMTP 
        private const string SmtpServer = "smtp.gmail.com"; // Sostituisci con il tuo server SMTP
        private const int SmtpPort = 587; // Sostituisci con la porta del tuo server SMTP
        private const string SmtpUser = "lucos.maio@gmail.com"; // Sostituisci con il tuo nome utente SMTP
        private const string SmtpPass = "ieaa mkfg mqsb ugmh"; // Sostituisci con la tua password SMTP

        // Costruttori
        public Email()
        {
            mail.From = new MailAddress(SmtpUser);
            mail.To.Add(Destinatario);
            mail.Subject = Oggetto;
            mail.Body = Corpo;

            Destinatario = string.Empty;
            Oggetto = string.Empty;
            Corpo = string.Empty;
        }

        public Email(string destinatario, string oggetto, string corpo)
        {
            Destinatario = destinatario;
            Oggetto = oggetto;
            Corpo = corpo;

            mail.From = new MailAddress(SmtpUser);
            mail.To.Add(Destinatario);
            mail.Subject = Oggetto;
            mail.Body = Corpo;
        }

        // Funzione che verifica se l'email è valida
        public bool EmailValida()
        {
            // try
            // {
            //     var addr = new MailAddress(Destinatario);
            //     return addr.Address == Destinatario;
            // }
            // catch
            // {
            //     return false;
            // }
            // Verifico se l'email contiene una chiocciola e un punto
            return Destinatario.Contains("@") && Destinatario.Contains(".");
        }

        // Invia l'email
        public void Invia()
        {

            // Verifico che l'oggetto, il corpo e l'email non siano vuoti
            if(string.IsNullOrWhiteSpace(Oggetto) || string.IsNullOrWhiteSpace(Corpo) || string.IsNullOrWhiteSpace(Destinatario))
            {
                throw new ArgumentException("L'oggetto, il corpo e l'email non possono essere vuoti.");
            }
            
            // Verifico se l'email è valida
            if(!EmailValida())
            {
                throw new ArgumentException("L'indirizzo email del destinatario non è valido.");
            }

            using (SmtpClient smtp = new SmtpClient(SmtpServer, SmtpPort))
            {
                smtp.Credentials = new NetworkCredential(SmtpUser, SmtpPass);
                smtp.EnableSsl = true;
                smtp.Send(mail);
            }
        }

        // Override del metodo ToString per una rappresentazione leggibile dell'email
        public override string ToString()
        {
            return $"A: {Destinatario}\nOggetto: {Oggetto}\nCorpo: {Corpo}";
        }
    }
}