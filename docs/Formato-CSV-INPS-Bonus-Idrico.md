# Formato CSV - INPS Bonus Idrico

Funzione di lettura: `CSVReader.LeggiFileINPS`.

Il file deve avere almeno 16 campi, quindi posizioni da 1 a 16. Tutte le posizioni vengono lette dal codice.

## Ordine Campi

| Posizione | Indice codice | Campo | Obbligatorio per il caricamento | Note |
| --- | ---: | --- | --- | --- |
| 1 | 0 | ID ATO | Si | Identificativo ATO. |
| 2 | 1 | Codice Bonus | Si | Deve avere 15 caratteri. |
| 3 | 2 | Codice Fiscale Richiedente | Si | Salvato in maiuscolo. |
| 4 | 3 | Nome Dichiarante | Si | Salvato in maiuscolo. |
| 5 | 4 | Cognome Dichiarante | Si | Salvato in maiuscolo. |
| 6 | 5 | Codici Fiscali Familiari | No | Separati da virgola. Il codice li legge ma attualmente non li usa nel confronto principale. |
| 7 | 6 | Anno Validita | Si | Anno del bonus. |
| 8 | 7 | Data Inizio Validita | Si | Data `dd/MM/yyyy` o `yyyy-MM-dd`. |
| 9 | 8 | Data Fine Validita | Si | Data `dd/MM/yyyy` o `yyyy-MM-dd`. |
| 10 | 9 | Indirizzo Abitazione | Si | Salvato in maiuscolo. |
| 11 | 10 | Numero Civico | Si | Viene normalizzato mantenendo la parte numerica. |
| 12 | 11 | ISTAT Abitazione | Si | Deve avere 6 caratteri. |
| 13 | 12 | CAP Abitazione | Si | Deve avere 5 caratteri. |
| 14 | 13 | Provincia Abitazione | Si | Deve avere 2 caratteri. |
| 15 | 14 | Presenza POD | Si | Valori ammessi: `SI` o `NO`. |
| 16 | 15 | Numero Componenti | Si | Deve essere numerico. |

Esempio intestazione minima coerente:

```csv
IdAto;CodiceBonus;CodiceFiscaleRichiedente;NomeDichiarante;CognomeDichiarante;CodiciFiscaliFamiliari;AnnoValidita;DataInizioValidita;DataFineValidita;IndirizzoAbitazione;NumeroCivico;IstatAbitazione;CapAbitazione;ProvinciaAbitazione;PresenzaPod;NumeroComponenti
```

