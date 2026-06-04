# Gestione Dei Toponimi

Questo documento descrive come l'applicazione gestisce i toponimi, cioe' le denominazioni stradali usate per collegare gli indirizzi delle utenze idriche agli indirizzi dell'anagrafe e dei file INPS.

## Scopo

I toponimi servono a ridurre le differenze formali fra indirizzi scritti in modi diversi. Per esempio abbreviazioni, accenti, punti, spazi, numeri civici scritti dentro l'indirizzo e iniziali di nomi propri vengono normalizzati prima del confronto.

La tabella principale e' `toponomi`, esposta nel codice tramite `DbSet<Toponimo> Toponomi`.

Campi principali:

- `denominazione`: forma base del toponimo.
- `normalizzazione`: forma standardizzata usata nei confronti.
- `tipoToponimo`: tipo rilevato, per esempio `VIA`, `PIAZZA`, `CORSO`.
- `intestazione`: parte del nome dopo il tipo.
- `intestazioneNormalizzata`: intestazione derivata dalla normalizzazione.
- `IdEnte`: ente proprietario del toponimo.
- `dataCreazione` e `dataAggiornamento`: tracciamento delle modifiche.

## Normalizzazione

La normalizzazione e' implementata in `Models/FunzioniTrasversali.cs`.

Le funzioni principali sono:

- `ExtractToponimoAndCivico`: separa il toponimo dal numero civico, anche quando il civico e' scritto in coda all'indirizzo.
- `NormalizeToponimo`: trasforma il toponimo in una forma confrontabile.
- `NormalizeIndirizzoCompleto`: normalizza toponimo, civico, scala, piano e interno.
- `AreToponimiCompatibili`: riconosce casi compatibili anche quando una parte del nome e' indicata con iniziale.
- `AnalizzaIndirizzoPerToponimo`: estrae tipo e intestazione.

Regole importanti:

- il testo viene portato in maiuscolo;
- vengono rimossi accenti, virgolette, punteggiatura e spazi doppi;
- abbreviazioni come `P.ZZA`, `PZA`, `C.SO`, `V.LE`, `VIC`, `CDA`, `SNC` vengono espanse;
- `SP` e `SS` vengono trasformate in `STRADA PROVINCIALE` e `STRADA STATALE`;
- i numeri ordinali vengono convertiti in parole quando coerenti con il tipo di toponimo;
- il numero civico viene separato dal toponimo e normalizzato a parte.

## Creazione E Modifica Manuale

La gestione manuale passa da `Controllers/ToponomiController.cs`.

Nel metodo `crea`:

1. l'utente inserisce denominazione e normalizzazione;
2. il sistema prova a estrarre un eventuale civico dalla denominazione;
3. calcola la normalizzazione;
4. salva il nuovo `Toponimo`;
5. svuota la cache dell'ente con `_cache.ClearEnteCache(idEnte)`.

Nel metodo `Update`:

1. recupera il toponimo esistente;
2. ricalcola denominazione e normalizzazione;
3. aggiorna `dataAggiornamento`;
4. salva e invalida la cache dell'ente.

Il civico estratto da un toponimo inserito manualmente non diventa parte della denominazione: viene rimosso dal testo stradale e registrato nei log.

## Creazione Durante Il Caricamento Utenze

La parte piu' importante e' nel caricamento delle utenze idriche, in `CSVReader.LeggiFileUtenzeIdriche`.

Per ogni riga del CSV utenze:

1. il sistema legge l'indirizzo di ubicazione e il civico;
2. chiama `ExtractToponimoAndCivico`;
3. calcola `indirizzoUbicazioneNormalizzato`;
4. prova a formattare l'indirizzo rispetto all'anagrafe con `FormattaIndirizzo`;
5. cerca un toponimo gia' presente per l'ente.

La ricerca usa tre livelli:

- confronto diretto sulla `denominazione`;
- confronto fra normalizzazioni di `denominazione` e `normalizzazione`;
- confronto tramite `AreToponimiCompatibili`, utile per iniziali di nomi propri.

Se trova un solo candidato compatibile, associa automaticamente l'utenza al toponimo. Se trova piu' candidati, non associa in automatico e scrive un warning, per evitare abbinamenti ambigui.

Se il toponimo esiste ma non ha `normalizzazione`, il caricamento prova a valorizzarla. Se il tipo toponimo e' cambiato o mancava, viene aggiornato. Se invece il toponimo non esiste, viene creato nella lista dei nuovi toponimi da salvare.

## Associazione Alle Utenze

Ogni utenza puo' avere `idToponimo`. Durante il caricamento, quando il toponimo viene trovato, l'id viene assegnato subito.

Dopo il salvataggio iniziale delle utenze, `UtenzeController.Upload` esegue anche un secondo passaggio sulle utenze dell'ente con `idToponimo == null`. Questo serve a recuperare associazioni che non erano disponibili al primo giro, per esempio perche' il toponimo e' appena stato creato.

Anche questo secondo passaggio usa:

- estrazione del civico;
- normalizzazione;
- confronto diretto;
- confronto compatibile su iniziali;
- log in caso di conflitto fra civico estratto e civico separato.

## Effetto Sull'Elaborazione INPS

I toponimi entrano nella verifica della fornitura quando l'indirizzo dell'utenza non coincide direttamente con la residenza del dichiarante.

In `FunzioniTrasversali.VerificaEsistenzaFornitura`, se la fornitura ha `idToponimo`, il sistema recupera il toponimo e confronta la sua `normalizzazione` con l'indirizzo di residenza o con l'indirizzo INPS. Questo permette di riconoscere forniture che sarebbero diverse solo per forma testuale.

## Cache

La lista dei toponimi per ente e' memorizzata in cache con chiave:

```text
toponimi:ente:{idEnte}
```

La cache dura 15 minuti. Ogni creazione, modifica o caricamento utenze che tocca i toponimi chiama `ClearEnteCache`, che rimuove anche questa chiave.

## Punti Di Attenzione

- Se un indirizzo contiene un civico diverso dal campo civico separato, il sistema registra un warning.
- Se piu' toponimi sono compatibili, l'associazione automatica viene evitata.
- La qualita' dei toponimi incide direttamente sugli esiti INPS, soprattutto sugli esiti `01` e `03`.
- Dopo un caricamento utenze con molti indirizzi malformati, conviene controllare i toponimi prima di lanciare elaborazioni INPS.
