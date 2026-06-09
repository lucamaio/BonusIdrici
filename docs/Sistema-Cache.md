# Sistema Di Cache

Il sistema usa una cache applicativa in memoria per ridurre query ripetute su dati letti spesso: enti, anagrafe, utenze, toponimi, report, statistiche e dashboard.

## Registrazione

La cache e' configurata in `Program.cs`.

Servizi registrati:

```csharp
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<AppCacheService>();
```

La classe centrale e' `Services/AppCacheService.cs`. E' un singleton che incapsula `IMemoryCache`.

## Durate

Le durate sono definite come costanti:

- `EntiExpiration`: 10 minuti;
- `ToponimiExpiration`: 15 minuti;
- `AnagrafeExpiration`: 5 minuti;
- `UtenzeExpiration`: 5 minuti;
- `ReportExpiration`: 5 minuti;
- `StatisticheExpiration`: 3 minuti.

Se una chiamata non passa una durata esplicita, viene usato il default di 5 minuti.

## Lettura Dalla Cache

Ci sono due metodi principali:

- `GetOrCreate`;
- `GetOrCreateAsync`.

`GetOrCreate` controlla se la chiave e' gia' presente. Se esiste, restituisce il valore e scrive un log debug `Cache HIT`. Se non esiste, esegue la factory, salva il risultato e scrive `Cache MISS`.

`GetOrCreateAsync` fa la stessa cosa ma usa un `SemaphoreSlim` per chiave. Questo evita che piu' richieste simultanee calcolino lo stesso dato nello stesso momento.

## Registro Delle Chiavi

`IMemoryCache` non espone direttamente l'elenco delle chiavi. Per questo `AppCacheService` mantiene un `ConcurrentDictionary<string, byte>` chiamato `_keys`.

Ogni volta che viene chiamato `Set`, la chiave viene aggiunta al dizionario. Questo permette di rimuovere le chiavi per prefisso.

## Rimozione

Metodi disponibili:

- `Remove(key)`: rimuove una singola chiave;
- `RemoveByPrefix(prefix)`: rimuove tutte le chiavi registrate che iniziano con un prefisso;
- `ClearEnteCache(idEnte)`: invalida tutti i dati collegati a un ente;
- `ClearUserCache(idUser)`: invalida dati legati a un utente;
- `ClearReportCache(idReport)`: invalida dettaglio report e domande;
- `ClearAll()`: svuota tutta la cache registrata.

## Convenzione Delle Chiavi

Le chiavi seguono un formato leggibile.

Esempi:

```text
enti:all
enti:detail:{idEnte}
enti:user:{idUser}
anagrafe:ente:{idEnte}
utenze:ente:{idEnte}
toponimi:ente:{idEnte}
reports:ente:{idEnte}
statistiche:ente:{idEnte}
report:detail:{idReport}
domande:report:{idReport}
dashboard:admin
dashboard:user:{idUser}
```

Questa convenzione rende possibile invalidare intere aree con `RemoveByPrefix`.

## Invalidazione Per Ente

`ClearEnteCache(idEnte)` rimuove:

- dettaglio ente;
- anagrafe dell'ente;
- utenze dell'ente;
- toponimi dell'ente;
- report dell'ente;
- statistiche dell'ente;
- dashboard admin.

Viene chiamato dopo operazioni che cambiano dati dell'ente, per esempio:

- creazione o modifica toponimi;
- popolamento `VieEnte`;
- creazione o modifica `IndirizziNormalizzati`;
- creazione o modifica anagrafe;
- caricamento anagrafe;
- creazione o modifica utenze;
- caricamento utenze.

## Invalidazione Report

Dopo l'elaborazione INPS, `ElaborazioneController.Processing` chiama:

- `ClearReportCache(idReport)`;
- `RemoveByPrefix($"reports:ente:{selectedEnteId}")`;
- `RemoveByPrefix($"statistiche:ente:{selectedEnteId}")`;
- `Remove("dashboard:admin")`.

Questo evita che viste e statistiche mostrino dati precedenti al nuovo caricamento.

## Uso Nei Controller

Esempi principali:

- `AnagrafeController.Show`: cache `anagrafe:ente:{selectedEnteId}`;
- `UtenzeController.Show`: cache `utenze:ente:{selectedEnteId}`;
- `ToponomiController.Show`: cache `toponimi:ente:{selectedEnteId}`;
- `IndirizziNormalizzatiController.Show`: elenco `VieEnte` e `IndirizziNormalizzati` dell'ente;
- viste di selezione ente: cache `enti:all`;
- dettaglio nome ente: cache `enti:detail:{selectedEnteId}`.

Le query in cache usano spesso `AsNoTracking`, perche' i dati sono letti per visualizzazione e non devono essere tracciati da Entity Framework.

## Limiti

La cache e' in memoria del processo applicativo. Quindi:

- si svuota al riavvio dell'applicazione;
- non e' condivisa fra piu' istanze dell'app;
- non sostituisce il database;
- non garantisce persistenza.

## Punti Di Attenzione

- Ogni modifica ai dati deve invalidare le chiavi coerenti.
- Se si aggiungono nuove viste cacheate, scegliere chiavi con prefissi coerenti.
- Se una vista mostra dati vecchi, controllare prima che il controller stia chiamando `ClearEnteCache`, `ClearReportCache` o `RemoveByPrefix` nel punto giusto.
- Dopo modifiche a `VieEnte` o `IndirizziNormalizzati`, verificare anche la dashboard admin e le attivita' recenti.
- Per dati modificabili, preferire cache brevi e invalidazione esplicita.
