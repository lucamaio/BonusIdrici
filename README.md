# Bonus Idrici 2

Applicazione ASP.NET Core MVC per caricare anagrafiche comunali, utenze idriche e file INPS del bonus idrico, confrontare i dati e produrre report/esportazioni.

## Scopo Del Sistema

Il sistema aiuta l'ente o l'operatore a:

- gestire enti, utenti e autorizzazioni;
- importare l'anagrafe dei residenti;
- importare e aggiornare le utenze idriche;
- elaborare i file CSV INPS relativi alle richieste di bonus idrico;
- verificare se il richiedente o un componente del nucleo familiare ha una fornitura idrica diretta;
- generare file CSV di esito e supporto operativo.

## Tecnologie

- ASP.NET Core MVC su `.NET 9`.
- Entity Framework Core.
- Database configurato tramite `ApplicationDbContext`.
- Upload e download CSV con separatore `;`.
- Log applicativi in `wwwroot/log`.

## Flusso Operativo

1. Configurazione enti

   L'amministratore configura gli enti gestiti, includendo dati utili al confronto territoriale come ISTAT, CAP, provincia, partita IVA e serie.

2. Caricamento anagrafe

   Da `Carica Anagrafica` si importa il CSV dell'anagrafe. Il sistema inserisce nuovi dichiaranti o aggiorna quelli gia presenti per lo stesso ente.

3. Caricamento utenze idriche

   Da `Carica Utenze Idriche` si importa il CSV delle forniture idriche. Il sistema:

   - valida le colonne principali;
   - crea nuove utenze o aggiorna quelle esistenti;
   - associa l'utenza al dichiarante quando trova corrispondenza su cognome, nome, codice fiscale ed ente;
   - crea o aggiorna i toponimi legati agli indirizzi di ubicazione.

4. Elaborazione file INPS

   Da `Elaborazione` si carica il CSV INPS e si selezionano ente, mese, anno, serie e opzioni di confronto. Il sistema crea o aggiorna un report e genera le domande elaborate.

5. Verifica e report

   Le domande elaborate vengono confrontate con anagrafe e utenze. Il sistema assegna un esito, salva eventuali incongruenze e rende disponibili i report.

6. Esportazioni

   Dai report si possono scaricare file CSV per:

   - `Esito Bonus Idrico`;
   - `Esito Competenza Territoriale`;
   - `Siscom`;
   - `Debug`.

## Esiti Principali

Gli esiti usati nelle domande sono:

- `01`: fornitura diretta ammessa;
- `02`: fornitura indiretta ammessa;
- `03`: fornitura diretta non rispetta i requisiti;
- `04`: fornitura indiretta non rispetta i requisiti.

Il campo testuale `esitoStr` indica invece se la richiesta risulta territorialmente compatibile con l'ente selezionato.

## File CSV In Ingresso

La documentazione dei tracciati si trova in `docs`:

- `docs/Formato-CSV-Anagrafe.md`;
- `docs/Formato-CSV-Utenze-Idriche.md`;
- `docs/Formato-CSV-INPS-Bonus-Idrico.md`.

Regole comuni:

- i CSV devono usare `;` come separatore;
- la prima riga e' intestazione;
- l'ordine delle colonne e' importante;
- le date accettate sono `dd/MM/yyyy` oppure `yyyy-MM-dd`.

## File CSV In Uscita

Le esportazioni sono generate da `Models/CsvGenerator.cs`.

Tipi principali:

- Esito bonus idrico: `COD_BONUS_IDRICO`, `ESITO`, `COD_FORNITURA`, `CF`, `N_NUCLEO`;
- Competenza territoriale: `ID_RICHIESTA`, `ESITO`;
- Siscom: `ID_ACQUEDOTTO`, `ANNO`, `SERIE`, `NUMERO_COMPONENTI`, `MC`;
- Debug: esportazione estesa delle domande, utile per controlli e analisi.

## Log

I log principali sono in `wwwroot/log`:

- `utenti.log`: accessi e operazioni utente;
- `Elaborazione_Anagrafe.log`: caricamento anagrafiche;
- `Lettura_UtenzeIdriche.log`: caricamento utenze idriche;
- `Elaborazione_INPS.log`: elaborazione file INPS.

I warning sui CSV non sempre bloccano il caricamento. Gli errori invece indicano righe saltate o dati insufficienti.

## Avvio E Build

Per compilare:

```powershell
dotnet build BonusIdrici2.sln
```

Per avviare in sviluppo:

```powershell
dotnet run --project BonusIdrici2.csproj
```

Se la build fallisce per file bloccati in `bin/Debug/net9.0`, chiudere l'istanza dell'applicazione gia in esecuzione e rilanciare il comando.

## Note Di Manutenzione

- Le modifiche ai tracciati CSV vanno riportate sia in `Models/CSVReader.cs` sia nei file in `docs`.
- La generazione dei report INPS dipende dalla qualita dei dati caricati in anagrafe e utenze.
- Prima di ricaricare un CSV corretto, controllare il log relativo al caricamento precedente per capire quali righe sono state scartate o segnalate.
