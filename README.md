# Bonus Idrici 2

Applicazione ASP.NET Core MVC per gestire il processo del bonus idrico: caricamento anagrafe comunale, caricamento utenze idriche, normalizzazione dei toponimi, elaborazione dei file INPS, produzione dei report ed esportazione degli esiti.

## Scopo

Il sistema aiuta amministratori e operatori a:

- gestire enti, utenti e autorizzazioni;
- importare anagrafiche comunali;
- importare forniture idriche;
- mantenere uno storico mensile di anagrafe e utenze;
- normalizzare gli indirizzi tramite toponimi;
- creare e verificare normalizzazioni condivise fra vie anagrafiche e vie utenze;
- elaborare le richieste INPS del bonus idrico;
- verificare forniture dirette e indirette;
- produrre report, statistiche ed esportazioni CSV.

## Tecnologia

- ASP.NET Core MVC su `.NET 9`.
- Entity Framework Core con provider MySQL.
- Sessione applicativa con cookie `.BonusIdrici.Session`.
- Cache in memoria tramite `IMemoryCache` e `AppCacheService`.
- CSV con separatore `;`.
- Log applicativi in `wwwroot/log`.

## Struttura Principale

- `Controllers/`: controller MVC per anagrafe, utenze, toponimi, indirizzi normalizzati, elaborazioni, report, utenti ed enti.
- `Models/`: entita', lettori CSV, generatori CSV e funzioni trasversali.
- `Data/ApplicationDbContext.cs`: mapping Entity Framework delle tabelle.
- `Services/`: cache applicativa, riepilogo attivita', normalizzazione indirizzi e pulizia log.
- `Views/`: pagine Razor.
- `docs/`: documentazione tecnica e tracciati CSV.

## Flusso Operativo

1. Configurare gli enti con dati territoriali: ISTAT, CAP, provincia, partita IVA e impostazioni operative.
2. Caricare l'anagrafe del mese di riferimento.
3. Caricare le utenze idriche dello stesso mese.
4. Popolare le `VieEnte` e creare o aggiornare gli `IndirizziNormalizzati`.
5. Controllare eventuali warning su indirizzi, vie ambigue e toponimi.
6. Elaborare il file INPS scegliendo ente, mese, anno e opzioni di confronto.
7. Verificare le domande con incongruenze.
8. Esportare gli esiti richiesti.

## Dati Correnti E Storico

Il progetto distingue fra dato corrente e snapshot storico.

Dato corrente:

- `dichiaranti`;
- `utenzeidriche`;
- `toponomi`;
- `VieEnte`;
- `IndirizziNormalizzati`;
- `reports`;
- `domande`.

Snapshot:

- `dichiaranti_snapshot`;
- `utenzeidriche_snapshot`.

Durante i caricamenti di anagrafe e utenze, il sistema aggiorna le tabelle correnti e sostituisce gli snapshot gia' presenti per lo stesso ente, mese e anno. Le elaborazioni INPS usano gli snapshot quando disponibili e ricadono sui dati correnti solo come fallback, marcando la domanda con note e incongruenze.

## Elaborazione INPS

L'elaborazione parte da `ElaborazioneController.Processing` e usa `CSVReader.LeggiFileINPS`.

Il periodo del report viene preso dal nome file se contiene `_BID_YYYYMM`; altrimenti viene usato il periodo scelto nel form.

Per ogni richiesta il sistema:

- valida i campi obbligatori del tracciato INPS;
- controlla la competenza territoriale su ISTAT, CAP e provincia;
- cerca il richiedente nello snapshot anagrafico del periodo;
- verifica la fornitura nello snapshot utenze del periodo;
- confronta gli indirizzi usando prima `VieEnte` e `IndirizziNormalizzati`, poi il fallback testuale;
- valuta eventuali componenti maggiorenni del nucleo;
- calcola l'esito;
- calcola i metri cubi per esiti favorevoli;
- salva o aggiorna la domanda del report.

Esiti:

- `01`: fornitura diretta ammessa;
- `02`: fornitura indiretta ammessa;
- `03`: fornitura diretta trovata ma non conforme;
- `04`: nessuna fornitura utile o fornitura indiretta non ammessa.

## Toponimi

I toponimi normalizzano gli indirizzi e rendono piu' robusto il confronto fra anagrafe, utenze e INPS.

La logica principale e' in `FunzioniTrasversali`:

- separazione del civico dal toponimo;
- espansione abbreviazioni;
- rimozione accenti e punteggiatura;
- normalizzazione di ordinali;
- confronto fra toponimi compatibili.

I toponimi possono essere gestiti manualmente oppure creati e aggiornati durante il caricamento delle utenze idriche.

## Indirizzi Normalizzati

La normalizzazione moderna passa da due tabelle:

- `VieEnte`: raccoglie le vie lette da anagrafe e utenze, con fonte, occorrenze, civico eventualmente estratto e stato operativo;
- `IndirizziNormalizzati`: rappresenta la forma unica a cui collegare piu' varianti della stessa via.

La sezione `Indirizzi Normalizzati` permette di popolare le vie dell'ente, creare normalizzazioni automatiche, modificare una normalizzazione e vedere tutte le `VieEnte` collegate. Le abbreviazioni con iniziali, come `VIA B CAPUTO`, vengono collegate automaticamente solo quando esiste una corrispondenza univoca, per esempio `VIA BENEDETTO CAPUTO`; se ci sono piu' candidati restano in stato `AMBIGUA`.

## Cache

`AppCacheService` gestisce una cache in memoria con chiavi leggibili, per esempio:

- `enti:all`;
- `enti:detail:{idEnte}`;
- `anagrafe:ente:{idEnte}`;
- `utenze:ente:{idEnte}`;
- `toponimi:ente:{idEnte}`;
- `reports:ente:{idEnte}`;
- `report:detail:{idReport}`;
- `domande:report:{idReport}`.

Le modifiche ai dati invalidano le chiavi tramite `ClearEnteCache`, `ClearUserCache`, `ClearReportCache` o `RemoveByPrefix`.

## Documentazione

Documenti tecnici:

- `docs/Gestione-Toponimi.md`;
- `docs/Gestione-Indirizzi-Normalizzati.md`;
- `docs/Processo-Richieste-INPS.md`;
- `docs/Storico-Anagrafe-Utenze.md`;
- `docs/Sistema-Cache.md`;
- `docs/Caricamenti-Anagrafe-Utenze.md`.

Tracciati CSV:

- `docs/README-Formati-CSV.md`;
- `docs/Formato-CSV-Anagrafe.md`;
- `docs/Formato-CSV-Utenze-Idriche.md`;
- `docs/Formato-CSV-INPS-Bonus-Idrico.md`.

## Log

I log principali sono:

- `wwwroot/log/utenti.log`;
- `wwwroot/log/Elaborazione_Anagrafe.log`;
- `wwwroot/log/Elaborazione_Utenze.log`;
- `wwwroot/log/Elaborazione_INPS.log`;
- `wwwroot/log/IndirizziNormalizzati.log`.

I warning non bloccano sempre il caricamento. Gli errori indicano righe saltate o dati insufficienti.

La pagina `Attivita` dell'amministratore mostra le ultime 5 operazioni per categoria applicativa e le ultime 5 righe diagnostiche per livello `ERROR`, `WARNING`, `INFO` e `DEBUG`. I pulsanti di dettaglio aprono tabelle complete con paginazione.

## Build E Avvio

Compilazione:

```powershell
dotnet build BonusIdrici2.sln
```

Avvio in sviluppo:

```powershell
dotnet run --project BonusIdrici2.csproj
```

Se la build fallisce per file bloccati in `bin/Debug/net9.0`, chiudere l'istanza dell'applicazione gia' in esecuzione e rilanciare il comando.

## Manutenzione

- Le modifiche ai tracciati CSV vanno riportate sia in `Models/CSVReader.cs` sia nei documenti in `docs`.
- Prima di elaborare INPS, caricare anagrafe e utenze del periodo corretto.
- Se un'elaborazione usa fallback sui dati correnti, controllare le note e le incongruenze.
- Dopo modifiche a dati cacheati, verificare che venga invalidata la cache corretta.
- La qualita' dei toponimi incide direttamente sulla qualita' degli esiti.
