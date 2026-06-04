# Storico Di Anagrafe E Utenze Idriche

Lo storico dell'applicazione e' basato su snapshot mensili. Gli snapshot fotografano anagrafe e utenze idriche per un ente, un anno e un mese di riferimento.

## Perche' Esiste Lo Storico

Le elaborazioni INPS devono poter usare la situazione valida nel periodo della richiesta, non necessariamente il dato corrente del giorno in cui l'operatore elabora il file.

Per questo il sistema mantiene:

- dati correnti, usati per consultazione e gestione ordinaria;
- snapshot, usati come riferimento storico nelle elaborazioni.

## Tabelle Coinvolte

Anagrafe corrente:

```text
dichiaranti
```

Snapshot anagrafe:

```text
dichiaranti_snapshot
```

Utenze correnti:

```text
utenzeidriche
```

Snapshot utenze:

```text
utenzeidriche_snapshot
```

Nel codice sono esposte da `ApplicationDbContext` come:

- `Dichiaranti`;
- `DichiarantiSnapshot`;
- `UtenzeIdriche`;
- `UtenzeIdricheSnapshot`.

## Snapshot Anagrafe

Il modello e' `Models/DichiaranteSnapshot.cs`.

Contiene i dati anagrafici necessari per ricostruire il nucleo nel mese:

- ente e utente importatore;
- anno e mese di riferimento;
- codice fiscale, cognome, nome, sesso e data nascita;
- comune nascita;
- indirizzo e numero civico di residenza;
- parentela;
- codice famiglia;
- codice abitante;
- numero componenti;
- codice fiscale intestatario scheda;
- data cancellazione;
- data importazione;
- hash record.

Gli indici principali sono:

- periodo per ente;
- codice fiscale per ente e periodo, univoco;
- codice famiglia per ente e periodo;
- hash record.

## Snapshot Utenze

Il modello e' `Models/UtenzaIdricaSnapshot.cs`.

Contiene i dati di fornitura necessari per la verifica storica:

- ente e utente importatore;
- anno e mese di riferimento;
- id dell'utenza originale, se esiste;
- id acquedotto;
- matricola contatore;
- stato;
- periodo iniziale e finale;
- indirizzo, civico, sub, scala, piano, interno;
- tipo utenza;
- intestatario, codice fiscale e partita IVA;
- id toponimo;
- id dichiarante;
- data importazione;
- hash record.

Gli indici principali permettono ricerche per:

- periodo per ente;
- codice fiscale;
- id acquedotto;
- matricola;
- nominativo e data nascita;
- hash record.

## Creazione Degli Snapshot

Gli snapshot vengono creati durante i caricamenti CSV.

Nel caricamento anagrafe:

- `AnagrafeController.Upload` riceve `meseRiferimento` e `annoRiferimento`;
- `CSVReader.LoadAnagrafe` crea un `DichiaranteSnapshot` per ogni riga valida;
- prima del salvataggio vengono rimossi gli snapshot gia' esistenti per stesso ente, anno e mese;
- vengono inseriti i nuovi snapshot del periodo.

Nel caricamento utenze:

- `UtenzeController.Upload` riceve `meseRiferimento` e `annoRiferimento`;
- `CSVReader.LeggiFileUtenzeIdriche` crea un `UtenzaIdricaSnapshot` per ogni utenza valida;
- prima del salvataggio vengono rimossi gli snapshot utenze gia' esistenti per stesso ente, anno e mese;
- vengono inseriti i nuovi snapshot del periodo.

Questo significa che ricaricare lo stesso periodo sostituisce la fotografia precedente per quell'ente, mese e anno.

## Hash Record

Ogni snapshot salva `HashRecord`, calcolato con SHA-256 sui campi rilevanti.

L'hash non viene usato come chiave primaria, ma serve come impronta del contenuto importato. E' utile per controlli, confronti futuri e diagnosi.

## Dato Corrente E Dato Storico

Il caricamento non crea solo snapshot. Aggiorna anche le tabelle correnti:

- nuovi dichiaranti vengono inseriti in `dichiaranti`;
- dichiaranti gia' presenti vengono aggiornati campo per campo;
- nuove utenze vengono inserite in `utenzeidriche`;
- utenze gia' presenti vengono aggiornate campo per campo.

Lo snapshot resta invece la fotografia del periodo selezionato. Non rappresenta necessariamente lo stato corrente dopo successive importazioni.

## Uso Nelle Elaborazioni INPS

`CSVReader.LeggiFileINPS` cerca gli snapshot dell'ente per anno e mese del report.

Se trova snapshot anagrafica:

- cerca il richiedente nello snapshot;
- ricostruisce il dichiarante a partire dallo snapshot;
- calcola il nucleo familiare storico con `TrovaNucleoSnapshot`;
- esclude soggetti cancellati prima dell'inizio del mese report;
- calcola il numero componenti storico.

Se non trova snapshot anagrafica:

- usa l'anagrafe corrente;
- aggiunge una nota di avviso;
- marca la domanda come da verificare.

Se trova snapshot utenze:

- cerca le forniture nella snapshot del periodo;
- controlla validita' alla fine del mese report;
- usa stato, periodo, tipo utenza e indirizzo della fotografia storica.

Se non trova snapshot utenze:

- usa le utenze correnti;
- aggiunge una nota di avviso;
- marca la domanda come da verificare.

## Validita' Storica Delle Utenze

Una fornitura snapshot e' valida nel periodo se:

- `Stato` e' fra 1 e 3;
- `PeriodoIniziale` e' nullo oppure precedente o uguale alla data di riferimento;
- `PeriodoFinale` e' nullo oppure successivo o uguale alla data di riferimento.

La data di riferimento e' l'ultimo giorno del mese del report.

## Fallback

Il fallback su dati correnti e' intenzionale: evita che l'elaborazione si blocchi quando manca una fotografia storica. Pero' il sistema scrive note e incongruenze per segnalare che l'esito non e' completamente storico.

## Punti Di Manutenzione

- Prima di elaborare un file INPS di un periodo, caricare anagrafe e utenze dello stesso mese e anno.
- Ricaricare un periodo sostituisce gli snapshot precedenti per quel periodo.
- Se gli snapshot mancano, controllare le note delle domande prima di esportare esiti definitivi.
- Gli snapshot non vengono modificati dalle modifiche manuali successive: quelle modifiche impattano il dato corrente, non la fotografia gia' salvata.
