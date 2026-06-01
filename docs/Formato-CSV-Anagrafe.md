# Formato CSV - Anagrafe

Funzione di lettura: `CSVReader.LoadAnagrafe`.

Il file deve avere almeno 16 campi, quindi posizioni da 1 a 16. I campi non usati dal codice devono comunque restare presenti, anche vuoti, per non spostare le colonne successive.

## Ordine Campi

| Posizione | Indice codice | Campo | Obbligatorio per il caricamento | Note |
| --- | ---: | --- | --- | --- |
| 1 | 0 | Cognome | Si | Salvato in maiuscolo. |
| 2 | 1 | Nome | Si | Salvato in maiuscolo. |
| 3 | 2 | Codice Fiscale | Si | Deve avere 16 caratteri. |
| 4 | 3 | Sesso | Si | Valori previsti: `M` o `F`. |
| 5 | 4 | Data Nascita | Si a livello modello | Data `dd/MM/yyyy` o `yyyy-MM-dd`; se non valida viene usato `DateTime.MinValue`. |
| 6 | 5 | Comune Nascita | No | Salvato in maiuscolo. |
| 7 | 6 | Campo non letto | No | Deve restare presente per mantenere l'ordine. |
| 8 | 7 | Indirizzo Residenza | Si | Salvato in maiuscolo. |
| 9 | 8 | Numero Civico | Si | Viene normalizzato mantenendo la parte numerica. |
| 10 | 9 | Campo non letto | No | Deve restare presente per mantenere l'ordine. |
| 11 | 10 | Parentela | No | Salvato in maiuscolo. |
| 12 | 11 | Codice Famiglia | Si | Deve essere numerico. |
| 13 | 12 | Codice Abitante | Si | Deve essere numerico. |
| 14 | 13 | Numero Componenti | Si | Deve essere numerico. |
| 15 | 14 | Codice Fiscale Intestatario Scheda | No | Salvato come stringa. |
| 16 | 15 | Data Cancellazione | No | Data `dd/MM/yyyy` o `yyyy-MM-dd`; se vuota resta `null`. |

Esempio intestazione minima coerente:

```csv
Cognome;Nome;CodiceFiscale;Sesso;DataNascita;ComuneNascita;Campo07;IndirizzoResidenza;NumeroCivico;Campo10;Parentela;CodiceFamiglia;CodiceAbitante;NumeroComponenti;CodiceFiscaleIntestatarioScheda;DataCancellazione
```

