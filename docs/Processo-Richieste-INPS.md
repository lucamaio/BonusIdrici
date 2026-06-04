# Processo Delle Richieste INPS

Questo documento descrive come viene processato il file CSV fornito da INPS per il bonus idrico.

## Punto Di Ingresso

Il flusso parte da `Controllers/ElaborazioneController.cs`.

La pagina `NewProcessing` permette di scegliere:

- file CSV INPS;
- ente;
- mese;
- anno;
- serie;
- opzione di confronto del numero civico;
- opzione per escludere il controllo del numero componenti.

Il metodo `Processing` valida la sessione, il file e l'ente. Il file viene salvato temporaneamente con `Path.GetTempFileName`, elaborato e poi cancellato nel blocco `finally`.

## Periodo Del Report

Prima di leggere il CSV, il sistema determina il periodo del report con `DeterminaPeriodoReport`.

Se il nome file contiene una parte nel formato:

```text
_BID_YYYYMM
```

allora anno e mese vengono presi dal nome file. Se il pattern non e' presente o non e' valido, vengono usati mese e anno selezionati nel form.

Questo periodo e' importante perche' guida l'uso degli snapshot storici di anagrafe e utenze.

## Creazione O Riutilizzo Del Report

Il sistema cerca un report gia' presente con stesso ente, mese e anno.

Se esiste, riusa il suo `id`. Se non esiste, crea un nuovo `Report` con:

- `mese`;
- `anno`;
- `stato = "Da verificare"`;
- `serie`;
- `idUser`;
- `idEnte`;
- `DataCreazione`.

Le domande elaborate vengono collegate a questo report tramite `idReport`.

## Lettura Del CSV

La lettura vera avviene in `CSVReader.LeggiFileINPS`.

Il file usa `;` come delimitatore e la prima riga e' intestazione. Per ogni riga il sistema valida almeno 16 campi.

Campi principali letti:

- `ID_ATO`;
- `COD_BONUS_IDRICO`;
- `CF_DICHIARANTE`;
- nome e cognome dichiarante;
- codici fiscali componenti nucleo;
- anno validita';
- data inizio e fine validita';
- indirizzo e numero civico abitazione;
- ISTAT, CAP, provincia;
- presenza POD;
- numero componenti.

Le date INPS devono essere in formato `dd/MM/yyyy`.

## Validazioni

La riga viene scartata se contiene errori bloccanti, tra cui:

- tracciato con meno di 16 campi;
- `ID_ATO` mancante, non numerico o troppo lungo;
- `COD_BONUS_IDRICO` mancante o diverso da 15 caratteri;
- codice fiscale dichiarante presente ma diverso da 16 caratteri;
- nome o cognome mancanti;
- anno validita' mancante, non numerico o diverso da 4 caratteri;
- date di validita' non valide;
- indirizzo o civico mancanti;
- ISTAT, CAP o provincia non validi;
- presenza POD diversa da `SI` o `NO`;
- numero componenti mancante, non numerico o non positivo.

I codici fiscali dei componenti vengono validati come lista separata da virgole. Se un codice e' malformato, viene registrato errore sul campo `CF_COMPONENTI`.

Gli errori vengono scritti in `wwwroot/log/Elaborazione_INPS.log`.

## Uso Degli Snapshot

All'inizio dell'elaborazione il sistema cerca gli snapshot per ente, anno e mese:

- `DichiarantiSnapshot`;
- `UtenzeIdricheSnapshot`.

Se esiste lo snapshot anagrafico, il dichiarante viene cercato prima li'. Se manca il singolo codice fiscale nello snapshot, il sistema usa l'anagrafe corrente come fallback e marca la domanda come da verificare.

Se manca lo snapshot utenze del periodo, la verifica della fornitura usa le utenze correnti e aggiunge una nota di avviso.

Questo comportamento permette di elaborare comunque un file, ma rende esplicito quando l'esito non e' basato completamente sul dato storico del mese.

## Controllo Territoriale

Prima della verifica della fornitura, il sistema controlla che ISTAT, CAP e provincia della riga INPS coincidano con quelli dell'ente selezionato.

Se non coincidono:

- `esitoStr` resta `No`;
- viene aggiunta una nota;
- la domanda viene marcata con incongruenza.

Se coincidono e il dichiarante viene trovato:

- `esitoStr` diventa `Si`;
- viene avviata la verifica della fornitura diretta o indiretta.

## Verifica Della Fornitura

La verifica e' delegata a `FunzioniTrasversali.VerificaEsistenzaFornitura`.

Il sistema cerca una fornitura associata al dichiarante tramite:

- codice fiscale;
- oppure cognome, nome e data di nascita.

Con snapshot utenze attivo, la ricerca avviene nella snapshot del periodo. In assenza di snapshot, usa la tabella corrente `UtenzeIdriche`.

Una fornitura e' considerata favorevole se:

- e' domestica (`UTENZA DOMESTICA`);
- e' valida nel periodo del report;
- ha stato valido, cioe' fra 1 e 3;
- l'indirizzo coincide con la residenza o con l'indirizzo INPS, eventualmente usando normalizzazione e toponimi;
- se richiesto, coincide anche il numero civico.

Se la fornitura trovata non e' domestica, non e' valida nel periodo o ha indirizzo incoerente, l'esito diventa `03`.

## Componenti Del Nucleo

Se il dichiarante non ha una fornitura diretta, il sistema valuta i componenti maggiorenni del nucleo.

Con snapshot anagrafica disponibile, i familiari vengono cercati nello snapshot del periodo usando:

- `CodiceFamiglia`;
- oppure `CodiceFiscaleIntestatarioScheda`;
- escludendo soggetti cancellati prima dell'inizio del mese report.

Senza snapshot, la ricerca usa l'anagrafe corrente.

Se un familiare ha una fornitura valida, l'esito diventa `01` e viene salvato il codice fiscale della persona la cui utenza e' stata trovata.

Se nessuna fornitura diretta e' valida ma `Presenza POD = SI`, l'esito puo' diventare `02`.

## Esiti

Gli esiti salvati in `Domanda.esito` sono:

- `01`: fornitura diretta ammessa;
- `02`: fornitura indiretta ammessa;
- `03`: fornitura diretta trovata ma non conforme;
- `04`: nessuna fornitura utile e fornitura indiretta non ammessa.

Il campo `esitoStr` indica la compatibilita' territoriale del richiedente rispetto all'ente selezionato.

## Metri Cubi

Per esiti `01` o `02`, il sistema calcola `mc` con:

```text
(50 * giorniBonus * numeroComponenti) / 1000
```

Il numero di giorni deriva da `dataFineValidita - dataInizioValidita`. Se il periodo e' negativo, i giorni vengono portati a zero.

## Salvataggio

Per ogni riga valida viene creato un oggetto `Domanda`.

Se non esiste una domanda con stesso `codiceBonus` e stesso `idReport`, viene aggiunta. Se esiste, il sistema confronta i campi principali e la aggiunge alla lista degli aggiornamenti solo se ci sono differenze.

Il controller salva aggiunte e aggiornamenti dentro una transazione. Dopo il commit invalida le cache:

- dettaglio report;
- domande del report;
- report dell'ente;
- statistiche dell'ente;
- dashboard admin.

## Note E Incongruenze

La domanda viene marcata con `incongruenze = true` quando ci sono condizioni da verificare, per esempio:

- snapshot mancanti;
- differenza fra numero componenti INPS e anagrafe;
- dati territoriali non coerenti;
- fornitura trovata ma con requisiti non conformi;
- fallback su dati correnti.

Le spiegazioni vengono salvate nel campo `note`.
