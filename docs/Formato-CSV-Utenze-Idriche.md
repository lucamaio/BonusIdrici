# Formato CSV - Utenze Idriche

Funzione di lettura: `CSVReader.LeggiFileUtenzeIdriche`.

Questo documento descrive il tracciato del CSV `RicercaParametricaH2O`, usato per caricare o aggiornare le utenze idriche. Il file di esempio analizzato contiene 83 colonne, ma la lettura dei campi usati dal sistema e' ora guidata dalle intestazioni quando sono presenti.

## Regole Generali

- Separatore campi: `;`.
- Prima riga: intestazione, viene usata per cercare i campi e poi saltata.
- Ordine colonne: consigliato come tracciato storico, ma non piu' vincolante per i campi riconosciuti da intestazione.
- Date accettate dal codice: `dd/MM/yyyy` oppure `yyyy-MM-dd`.
- Il parser rimuove eventuali virgolette doppie dai valori.
- Evitare valori contenenti `;`, perche' la lettura usa `Split(';')`.
- Il caricamento richiede almeno 39 colonne, ma il tracciato completo storico per `RicercaParametricaH2O` ne contiene 83.
- Se una intestazione riconosciuta viene trovata, il codice usa quella posizione; se non viene trovata, usa l'indice storico.

## Intestazioni Riconosciute

Per le utenze idriche il codice cerca queste intestazioni:

| Campo applicativo | Intestazione principale | Alternative |
| --- | --- | --- |
| Id Acquedotto | `IDAcquedotto` | - |
| Stato | `Stato` | - |
| Matricola Contatore | `MatContatore` | - |
| Periodo Iniziale | `PeriodoInizio` | - |
| Periodo Finale | `PeriodoFine` | - |
| Indirizzo Ubicazione | `ToponimiUbiDescrizione` | - |
| Numero Civico | `NCivico` | - |
| Sub | `Sub` | - |
| Scala | `Scala` | - |
| Piano | `Piano` | - |
| Interno | `Interno` | - |
| Tipo Utenza | `CategoriaDDesCategoria` | `TipoUtenzaDom` |
| Cognome / Ragione Sociale | `AnagraficaCognome` | - |
| Nome | `AnagraficaNome` | - |
| Sesso | `Sesso` | - |
| Data Nascita | `DataNascita` | - |
| Codice Fiscale | `CodiceFiscale` | - |
| Partita IVA | `PartitaIVA` | - |

Le intestazioni vengono normalizzate prima del confronto, quindi spazi, punteggiatura e differenze di maiuscole/minuscole non sono rilevanti.

## Colonne Usate Dal Sistema

Queste colonne alimentano direttamente il salvataggio in `UtenzaIdrica`, la creazione/normalizzazione dei toponimi e i successivi confronti con le domande INPS.

| Posizione | Indice codice | Intestazione | Campo applicativo | Obbligatorio | Note |
| --- | ---: | --- | --- | --- | --- |
| 1 | 0 | IDAcquedotto | Id Acquedotto | Si | Identificativo della fornitura idrica. |
| 10 | 9 | Stato | Stato | Si | Intero. Se vale `4` o `5`, alcune assenze vengono tollerate. |
| 13 | 12 | MatContatore | Matricola Contatore | Si, salvo casi particolari | Richiesta se `Stato` non e' `4`/`5` e `PeriodoFine` e' vuoto. |
| 14 | 13 | PeriodoInizio | Periodo Iniziale | Si | Data inizio validita fornitura. |
| 15 | 14 | PeriodoFine | Periodo Finale | No | Data fine validita fornitura. |
| 17 | 16 | ToponimiUbiDescrizione | Indirizzo Ubicazione | Si | Usato per collegare/creare toponimi. |
| 18 | 17 | NCivico | Numero Civico Ubicazione | Si | Viene normalizzato con `FormattaNumeroCivico`. |
| 19 | 18 | Sub | Sub Ubicazione | No | Salvato su `subUbicazione`. |
| 20 | 19 | Scala | Scala Ubicazione | No | Salvato su `scalaUbicazione`. |
| 21 | 20 | Piano | Piano | No | Salvato su `piano`. |
| 22 | 21 | Interno | Interno | No | Salvato su `interno`. |
| 28 | 27 | CategoriaDDesCategoria | Tipo Utenza | Si | Esempio: `UTENZA COMMERCIALE`. |
| 34 | 33 | AnagraficaCognome | Cognome / Ragione Sociale | Si | Per soggetti `D` contiene la ragione sociale. |
| 35 | 34 | AnagraficaNome | Nome | Si per persone fisiche | Per ditte puo' essere vuoto. |
| 36 | 35 | Sesso | Sesso / Tipo soggetto | Si | Valori ammessi: `M`, `F`, `D`. |
| 37 | 36 | DataNascita | Data Nascita | No bloccante | Se manca per `M`/`F`, viene scritto un warning ma la riga viene caricata. |
| 38 | 37 | CodiceFiscale | Codice Fiscale | Si per persone fisiche | Deve avere 16 caratteri se `Sesso` non e' `D`. |
| 39 | 38 | PartitaIVA | Partita IVA | Richiesta per ditte senza codice fiscale | Usata per soggetti `D`. |

## Struttura Completa Delle Colonne

| Posizione | Indice codice | Intestazione CSV | Usata dal caricamento | Note |
| --- | ---: | --- | --- | --- |
| 1 | 0 | IDAcquedotto | Si | Id fornitura. |
| 2 | 1 | AcquedottoGiriNumero | No | Mantenere la colonna. |
| 3 | 2 | MesiNoleggio | No | Mantenere la colonna. |
| 4 | 3 | LetturaPre | No | Mantenere la colonna. |
| 5 | 4 | LetturaAtt | No | Mantenere la colonna. |
| 6 | 5 | DataLetPre | No | Mantenere la colonna. |
| 7 | 6 | DataLetAtt | No | Mantenere la colonna. |
| 8 | 7 | Consumo | No | Mantenere la colonna. |
| 9 | 8 | ConsAgg | No | Mantenere la colonna. |
| 10 | 9 | Stato | Si | Stato utenza. |
| 11 | 10 | Contratto | No | Mantenere la colonna. |
| 12 | 11 | Cauzione | No | Mantenere la colonna. |
| 13 | 12 | MatContatore | Si | Matricola contatore. |
| 14 | 13 | PeriodoInizio | Si | Data inizio. |
| 15 | 14 | PeriodoFine | Si | Data fine, facoltativa. |
| 16 | 15 | Quantita | No | Colonna storica non usata. Nei file senza questa colonna il caricamento puo' comunque funzionare se le intestazioni dei campi usati sono presenti. |
| 17 | 16 | ToponimiUbiDescrizione | Si | Indirizzo ubicazione. |
| 18 | 17 | NCivico | Si | Numero civico ubicazione. |
| 19 | 18 | Sub | Si | Sub ubicazione. |
| 20 | 19 | Scala | Si | Scala ubicazione. |
| 21 | 20 | Piano | Si | Piano. |
| 22 | 21 | Interno | Si | Interno. |
| 23 | 22 | DataAggiornamento | No | Mantenere la colonna. |
| 24 | 23 | PercConsD | No | Mantenere la colonna. |
| 25 | 24 | PercConsC | No | Mantenere la colonna. |
| 26 | 25 | CategoriaCDesCategoria | No | Mantenere la colonna. |
| 27 | 26 | NumUtenzeC | No | Mantenere la colonna. |
| 28 | 27 | CategoriaDDesCategoria | Si | Tipo utenza. |
| 29 | 28 | NumUtenzeD | No | Mantenere la colonna. |
| 30 | 29 | CalcAcq | No | Mantenere la colonna. |
| 31 | 30 | CalcFog | No | Mantenere la colonna. |
| 32 | 31 | CalcDep | No | Mantenere la colonna. |
| 33 | 32 | CalcNol | No | Mantenere la colonna. |
| 34 | 33 | AnagraficaCognome | Si | Cognome o ragione sociale. |
| 35 | 34 | AnagraficaNome | Si | Nome. |
| 36 | 35 | Sesso | Si | `M`, `F`, `D`. |
| 37 | 36 | DataNascita | Si | Salvata se presente; warning non bloccante se manca per `M`/`F`. |
| 38 | 37 | CodiceFiscale | Si | Codice fiscale. |
| 39 | 38 | PartitaIVA | Si | Partita IVA. |
| 40 | 39 | ComuniNascitaNomeComune | No | Mantenere la colonna. |
| 41 | 40 | ComuniResidenzaNomeComune | No | Mantenere la colonna. |
| 42 | 41 | IndirizzoResidenza | No | Mantenere la colonna. |
| 43 | 42 | ToponimiResidenzaDescrizione | No | Mantenere la colonna. |
| 44 | 43 | NCivicoResidenza | No | Mantenere la colonna. |
| 45 | 44 | SubResidenza | No | Mantenere la colonna. |
| 46 | 45 | ScalaResidenza | No | Mantenere la colonna. |
| 47 | 46 | PianoResidenza | No | Mantenere la colonna. |
| 48 | 47 | InternoResidenza | No | Mantenere la colonna. |
| 49 | 48 | IndirizzoRecapito | No | Mantenere la colonna. |
| 50 | 49 | ToponimiRecapitoDescrizione | No | Mantenere la colonna. |
| 51 | 50 | NCivicoRecapito | No | Mantenere la colonna. |
| 52 | 51 | SubRecapito | No | Mantenere la colonna. |
| 53 | 52 | ScalaRecapito | No | Mantenere la colonna. |
| 54 | 53 | PianoRecapito | No | Mantenere la colonna. |
| 55 | 54 | InternoRecapito | No | Mantenere la colonna. |
| 56 | 55 | ComuniRecapitoNomeComune | No | Mantenere la colonna. |
| 57 | 56 | DataDecesso | No | Mantenere la colonna. |
| 58 | 57 | DataAggAnag | No | Mantenere la colonna. |
| 59 | 58 | AnagraficaCognomeRecapito | No | Mantenere la colonna. |
| 60 | 59 | AnagraficaNomeRecapito | No | Mantenere la colonna. |
| 61 | 60 | AcquedottoModelliDescrizione | No | Mantenere la colonna. |
| 62 | 61 | Note1 | No | Mantenere la colonna. |
| 63 | 62 | Sezione | No | Mantenere la colonna. |
| 64 | 63 | Foglio | No | Mantenere la colonna. |
| 65 | 64 | Numero | No | Mantenere la colonna. |
| 66 | 65 | Subalterno | No | Mantenere la colonna. |
| 67 | 66 | CodOrdinamento | No | Mantenere la colonna. |
| 68 | 67 | AcquedottoTabellaAddAccDescrizione | No | Mantenere la colonna. |
| 69 | 68 | Note | No | Mantenere la colonna. |
| 70 | 69 | IDCodiceABI | No | Mantenere la colonna. |
| 71 | 70 | IDCodiceCAB | No | Mantenere la colonna. |
| 72 | 71 | DescrizionePagamento | No | Mantenere la colonna. |
| 73 | 72 | QualificaTitUtenza | No | Mantenere la colonna. |
| 74 | 73 | TipoUtenzaDom | No | Mantenere la colonna. |
| 75 | 74 | CodAssenzaDatiCat | No | Mantenere la colonna. |
| 76 | 75 | DataAttivUtenza | No | Mantenere la colonna. |
| 77 | 76 | NTelefono | No | Mantenere la colonna. |
| 78 | 77 | NFax | No | Mantenere la colonna. |
| 79 | 78 | EMail | No | Mantenere la colonna. |
| 80 | 79 | ComuniRecapitoProvincia | No | Mantenere la colonna. |
| 81 | 80 | ComuniRecapitoCap | No | Mantenere la colonna. |
| 82 | 81 | CapRecapito | No | Mantenere la colonna. |
| 83 | 82 | Campo vuoto finale | No | Nel file esportato e' presente un separatore finale, quindi risulta una colonna vuota. |

## Errori E Warning Principali

| Messaggio | Campo da controllare |
| --- | --- |
| `Riga malformata. Attesi almeno 39` | Numero totale colonne: devono esserci almeno 39 campi separati da `;`. |
| `Id Acquedotto mancante` | Posizione 1, `IDAcquedotto`. |
| `Codice Fiscale mancante` | Posizione 38, `CodiceFiscale`, se il soggetto non ha `Sesso` = `D`. |
| `Codice Fiscale mal formato` | Posizione 38, `CodiceFiscale`: deve essere lungo 16 caratteri per persone fisiche. |
| `Codice Fiscale e Partita IVA della ditta ... non presente/trovati` | Posizioni 36, 38, 39; per ditte (`D`) manca sia codice fiscale valido sia partita IVA. |
| `Matricola Contatore mancante` | Posizione 13, `MatContatore`, salvo stato `4`/`5` o periodo finale valorizzato. |
| `Nome o Cognome mancante` | Posizioni 34 e 35, se non e' una ditta (`D`). |
| `Sesso mancante o mal formato` | Posizione 36, `Sesso`: ammessi solo `M`, `F`, `D`. |
| `Periodo iniziale mancante` | Posizione 14, `PeriodoInizio`. |
| `Tipo Utenza mancante` | Posizione 28, `CategoriaDDesCategoria`. |
| `Indirizzo ubicazione mancante` | Posizione 17, `ToponimiUbiDescrizione`. |
| `Data Nascita mancante` | Posizione 37, `DataNascita`; warning non bloccante per persone fisiche. |
| `Numero civico mancante` | Posizione 18, `NCivico`. |

## Compatibilita Con File Phiranha

Il caricamento e' compatibile con file Phiranha in cui la colonna `Quantita` non e' presente o alcune colonne non usate sono spostate. La condizione importante e' che le intestazioni dei campi usati siano presenti e che la riga abbia almeno 39 campi.

Se il sistema mostra `Nessun dato valido trovato nel file CSV`, controllare prima:

- presenza della riga di intestazione;
- separatore `;`;
- colonne `CodiceFiscale`, `Sesso`, `CategoriaDDesCategoria` o `TipoUtenzaDom`;
- colonne `ToponimiUbiDescrizione` e `NCivico`;
- eventuali errori in `wwwroot/log/Elaborazione_Utenze.log`.

## Intestazione Attesa

```csv
IDAcquedotto;AcquedottoGiriNumero;MesiNoleggio;LetturaPre;LetturaAtt;DataLetPre;DataLetAtt;Consumo;ConsAgg;Stato;Contratto;Cauzione;MatContatore;PeriodoInizio;PeriodoFine;Quantita;ToponimiUbiDescrizione;NCivico;Sub;Scala;Piano;Interno;DataAggiornamento;PercConsD;PercConsC;CategoriaCDesCategoria;NumUtenzeC;CategoriaDDesCategoria;NumUtenzeD;CalcAcq;CalcFog;CalcDep;CalcNol;AnagraficaCognome;AnagraficaNome;Sesso;DataNascita;CodiceFiscale;PartitaIVA;ComuniNascitaNomeComune;ComuniResidenzaNomeComune;IndirizzoResidenza;ToponimiResidenzaDescrizione;NCivicoResidenza;SubResidenza;ScalaResidenza;PianoResidenza;InternoResidenza;IndirizzoRecapito;ToponimiRecapitoDescrizione;NCivicoRecapito;SubRecapito;ScalaRecapito;PianoRecapito;InternoRecapito;ComuniRecapitoNomeComune;DataDecesso;DataAggAnag;AnagraficaCognomeRecapito;AnagraficaNomeRecapito;AcquedottoModelliDescrizione;Note1;Sezione;Foglio;Numero;Subalterno;CodOrdinamento;AcquedottoTabellaAddAccDescrizione;Note;IDCodiceABI;IDCodiceCAB;DescrizionePagamento;QualificaTitUtenza;TipoUtenzaDom;CodAssenzaDatiCat;DataAttivUtenza;NTelefono;NFax;EMail;ComuniRecapitoProvincia;ComuniRecapitoCap;CapRecapito;
```
