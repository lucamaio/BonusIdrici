using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Dichiarante
{
    public class Dichiarante
    {
        public int? IdDichiarante { get; set; }
        public string Cognome { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string CodiceFiscale { get; set; } = string.Empty;
        public string Sesso { get; set; } = string.Empty;
        public string DataNascita { get; set; } = string.Empty;
        public string ComuneNascita { get; set; } = string.Empty;
        public string IndirizzoResidenza { get; set; }  = string.Empty;
        public string NumeroCivico { get; set; }  = string.Empty;
        public string NomeEnte  { get; set; }  = string.Empty;
        
        public string NumeroComponenti { get; set; }  = string.Empty;
        
        public string CodiceFamiglia { get; set; } = string.Empty;
        public string Parentela { get; set; } = string.Empty;
        public string CodiceFiscaleIntestatarioScheda { get; set; } = string.Empty;

        // public string Cap { get; set; } = string.Empty;
        // public string Istat { get; set; }  = string.Empty;
        // public string ProvinciaAbitazione { get; set; } = string.Empty;
        // public List<string> CfMembri { get; set; } = new List<string>();// CamelCase per convenzione C#
    }
}