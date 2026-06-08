# Caricamenti Anagrafe E Utenze Idriche

Questo documento descrive come funzionano i caricamenti CSV di anagrafe e utenze idriche.

## Regole Comuni

Entrambi i caricamenti:

- sono riservati agli utenti con ruolo `ADMIN`;
- accettano solo file `.csv`;
- usano `;` come delimitatore;
- ignorano la prima riga, considerata intestazione;
- richiedono ente, mese e anno di riferimento;
- salvano temporaneamente il file con `Path.GetTempFileName`;
- cancellano il file temporaneo nel blocco `finally`;
- salvano i dati dentro una transazione;
- creano snapshot mensili;
- invalidano la cache dell'ente dopo il salvataggio.

Il mese deve essere fra 1 e 12. L'anno deve essere fra 2021 e l'anno corrente piu' uno.

## Caricamento Anagrafe

Il punto di ingresso e' `AnagrafeController.Upload`.

Il controller:

1. verifica ruolo `ADMIN`;
2. valida file, ente, mese e anno;
3. salva il file temporaneo;
4. chiama `CSVReader.LoadAnagrafe`;
5. salva nuovi dichiaranti, aggiornamenti e snapshot;
6. rimuove gli snapshot gia' presenti per stesso ente, mese e anno;
7. inserisce i nuovi snapshot;
8. invalida cache ente e cache utente.

## Validazioni Anagrafe

`CSVReader.LoadAnagrafe` richiede almeno 16 campi per riga.

Una riga viene scartata se mancano o sono malformati:

- cognome;
- nome;
- codice fiscale, che deve avere 16 caratteri;
- sesso;
- indirizzo residenza;
- numero civico.

I valori vengono normalizzati:

- cognome, nome, codice fiscale e sesso in maiuscolo;
- indirizzo in maiuscolo;
- numero civico con `FormattaNumeroCivico`;
- date con `ConvertiData`.

## Inserimento E Aggiornamento Anagrafe

Il dichiarante viene cercato per:

```text
CodiceFiscale + IdEnte
```

Se non esiste, viene aggiunto a `Dichiaranti`.

Se esiste, il sistema confronta i campi principali e lo aggiunge alla lista `DichiarantiDaAggiornare` solo se trova differenze.

Campi confrontati:

- cognome;
- nome;
- sesso;
- data nascita;
- comune nascita;
- indirizzo residenza;
- numero civico;
- parentela;
- codice famiglia;
- codice abitante;
- numero componenti;
- codice fiscale intestatario scheda;
- data cancellazione.

## Snapshot Anagrafe

Per ogni riga valida viene creato un `DichiaranteSnapshot`.

Prima di inserire i nuovi snapshot, il controller elimina quelli gia' presenti per:

```text
IdEnte + AnnoRiferimento + MeseRiferimento
```

Questo rende il caricamento del periodo sostitutivo: la fotografia precedente viene rimpiazzata da quella nuova.

## Caricamento Utenze Idriche

Il punto di ingresso e' `UtenzeController.Upload`.

Il controller:

1. verifica ruolo `ADMIN`;
2. valida file, ente, mese e anno;
3. salva il file temporaneo;
4. chiama `CSVReader.LeggiFileUtenzeIdriche`;
5. salva nuovi toponimi e aggiornamenti dei toponimi;
6. salva nuove utenze e aggiornamenti delle utenze esistenti;
7. sostituisce gli snapshot utenze del periodo;
8. invalida cache ente e cache utente;
9. esegue un secondo passaggio per associare eventuali utenze senza `idToponimo`.

## Validazioni Utenze

`CSVReader.LeggiFileUtenzeIdriche` richiede almeno 39 campi per riga.

Campi principali usati:

- `idAcquedotto`;
- stato;
- matricola contatore;
- periodo iniziale e finale;
- indirizzo ubicazione;
- numero civico;
- sub, scala, piano, interno;
- tipo utenza;
- cognome, nome, sesso;
- data nascita;
- codice fiscale;
- partita IVA.

Una riga puo' essere scartata per:

- codice fiscale mancante o malformato, salvo casi particolari di ditta;
- matricola mancante per utenze non cessate;
- nome/cognome mancanti per persone fisiche;
- sesso mancante o diverso da `M`, `F`, `D`;
- periodo iniziale mancante;
- tipo utenza mancante;
- indirizzo ubicazione mancante;
- numero civico mancante.

Alcune anomalie sono warning e non sempre bloccano il caricamento, per esempio data nascita mancante o id acquedotto mancante.

## Id Acquedotto Mancante

Se `idAcquedotto` manca, il sistema crea una chiave alternativa con `CreaChiaveAlternativaUtenza`.

La chiave parte da:

- codice fiscale;
- matricola;
- indirizzo;
- civico.

Il valore viene hashato con SHA-256 e prefissato con `ALT-`. Questa chiave permette almeno di creare una snapshot e gestire la riga senza un id acquedotto ufficiale.

## Toponimi Nel Caricamento Utenze

Durante il caricamento utenze, il sistema normalizza l'indirizzo di ubicazione e prova ad associarlo a un toponimo dell'ente.

Se il toponimo non esiste, viene creato. Se esiste ma mancano normalizzazione o tipo, viene aggiornato.

Se piu' toponimi risultano compatibili, l'associazione automatica viene evitata e viene scritto un warning.

## Inserimento E Aggiornamento Utenze

L'utenza viene cercata fra quelle gia' presenti per:

```text
idAcquedotto + codiceFiscale
```

Se non esiste, viene aggiunta a `UtenzeIdriche`.

Se esiste, il sistema confronta i campi e aggiorna solo se necessario. L'aggiornamento imposta `data_aggiornamento`.

L'utenza puo' essere collegata al dichiarante corrente tramite:

```text
cognome + nome + codice fiscale + ente
```

## Snapshot Utenze

Per ogni utenza valida viene creato un `UtenzaIdricaSnapshot`, evitando duplicati nella stessa importazione per stesso ente, anno, mese e id acquedotto.

Prima di inserire i nuovi snapshot, il controller elimina quelli gia' presenti per:

```text
IdEnte + AnnoRiferimento + MeseRiferimento
```

Anche il caricamento utenze e' quindi sostitutivo per periodo.

## Secondo Passaggio Toponimi

Dopo il salvataggio, il controller cerca le utenze dell'ente ancora senza `idToponimo`.

Per queste utenze:

- ricalcola toponimo e civico;
- ricarica i toponimi dell'ente dal database;
- cerca corrispondenze dirette, normalizzate o compatibili;
- assegna `idToponimo` se trova un solo risultato chiaro.

Questo passaggio recupera casi in cui il toponimo e' stato creato nello stesso caricamento e quindi non era ancora disponibile nel database al momento della prima associazione.

## Messaggi E Log

Log principali:

- `wwwroot/log/Elaborazione_Anagrafe.log`;
- `wwwroot/log/Elaborazione_Utenze.log`.

I messaggi mostrati all'utente riportano:

- numero di record aggiunti;
- numero di record aggiornati;
- numero di snapshot creati per mese e anno;
- presenza di indirizzi malformati nel caricamento utenze.

## Sequenza Consigliata

Per un'elaborazione INPS affidabile:

1. caricare l'anagrafe del mese corretto;
2. caricare le utenze idriche dello stesso mese;
3. controllare eventuali warning sui toponimi;
4. elaborare il file INPS dello stesso periodo;
5. verificare le domande marcate con incongruenze prima dell'esportazione.
