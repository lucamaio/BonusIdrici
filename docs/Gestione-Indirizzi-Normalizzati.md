# Gestione Degli Indirizzi Normalizzati

Questo documento descrive il flusso moderno di normalizzazione degli indirizzi. La logica affianca i toponimi legacy e viene usata per rendere piu' affidabile il confronto fra indirizzo di residenza anagrafica e indirizzo di ubicazione dell'utenza idrica.

## Tabelle

Le tabelle principali sono:

- `VieEnte`: contiene le vie lette dai dati dell'ente.
- `IndirizziNormalizzati`: contiene la forma unica usata nei confronti.

Una stessa normalizzazione puo' avere piu' `VieEnte` collegate. Per esempio `VIA B CAPUTO`, `VIA B. CAPUTO` e `VIA BENEDETTO CAPUTO` possono puntare allo stesso `IndirizzoNormalizzato` se la corrispondenza e' univoca.

## Popolamento VieEnte

Il popolamento passa da `VieEnteService.PopolaVieEnteAsync`.

Per impostazione predefinita vengono lette:

- le residenze correnti da `Dichiaranti`;
- le ubicazioni correnti da `UtenzeIdriche`.

Gli snapshot possono essere inclusi passando `includiSnapshot = true`, ma il flusso UI standard lavora sui dati correnti. Per ogni indirizzo il sistema:

1. pulisce spazi e maiuscole;
2. separa un eventuale civico scritto nel campo indirizzo;
3. calcola `DenominazionePulita`;
4. propone `DenominazioneNormalizzataProposta`;
5. raggruppa le occorrenze per denominazione e fonte;
6. inserisce nuove vie o aggiorna quelle gia' presenti.

Le vie senza denominazione utile vengono scartate. Le vie con civico estratto conservano una nota di diagnostica.

## Creazione Delle Normalizzazioni

La creazione passa da `VieEnteService.CreaIndirizziNormalizzatiAsync`.

Il servizio considera le `VieEnte` non ancora collegate con stato:

- `DA_ANALIZZARE`;
- `PROPOSTA`;
- `AMBIGUA`.

Per ogni gruppo normalizzato:

1. cerca un `IndirizzoNormalizzato` gia' esistente con la stessa denominazione;
2. se non esiste lo crea;
3. collega le vie del gruppo;
4. imposta lo stato della via a `COLLEGATA`.

## Abbreviazioni E Ambiguita

Le iniziali di nomi propri sono trattate con prudenza. Una via come `VIA B CAPUTO` contiene il token `B`, quindi puo' essere ambigua.

Il sistema prova comunque il collegamento automatico quando trova una sola normalizzazione compatibile. Esempi gestiti:

- `VIA B CAPUTO` puo' collegarsi a `VIA BENEDETTO CAPUTO` se e' l'unica candidata.
- `VIC I B CAPUTO` puo' collegarsi a `VICOLO I BENEDETTO CAPUTO`.
- `VIC`, `VICO` e `VICOLO` sono considerati equivalenti nel confronto.
- `CDA` e `C` iniziale sono normalizzati come `CONTRADA`.
- ordinali come `PRIMO`, `SECONDO`, `TERZO` possono essere confrontati con `I`, `II`, `III`.

Se i candidati sono piu' di uno, la via resta in stato `AMBIGUA` e deve essere verificata manualmente.

## Pagina Di Modifica

La pagina `IndirizziNormalizzati/Modifica` permette di:

- modificare denominazione normalizzata, stato, attivazione e note;
- visualizzare l'elenco completo delle `VieEnte` collegate;
- controllare fonte, denominazione originale, denominazione pulita, proposta normalizzata, stato e occorrenze.

Questa pagina serve a verificare che tutte le varianti reali della stessa via puntino alla stessa normalizzazione prima di elaborare o rieseguire i report.

## Uso Nel Confronto INPS

`INPSReaderNormalizzatoService` intercetta il confronto indirizzi eseguito durante l'elaborazione INPS.

La regola e':

1. cerca le due vie in `VieEnte` usando denominazione originale, pulita e proposta normalizzata;
2. se entrambe hanno `IdIndirizzoNormalizzato`, confronta gli ID;
3. se gli ID coincidono, gli indirizzi sono coerenti;
4. se manca una normalizzazione, usa il confronto testuale legacy come fallback e aggiunge una nota.

Il confronto del civico, quando attivo, viene controllato prima della normalizzazione della via.

## Log

Le operazioni della sezione sono tracciate in:

```text
wwwroot/log/IndirizziNormalizzati.log
```

La pagina `Attivita` mostra anche questi eventi nelle attivita' amministrative e li include nella diagnostica per livello.

## Punti Di Attenzione

- Dopo un caricamento anagrafe o utenze conviene ripopolare `VieEnte`.
- Dopo il popolamento conviene eseguire `Crea Indirizzi Normalizzati`.
- Le vie rimaste `AMBIGUA` vanno controllate prima di generare report definitivi.
- Una normalizzazione sbagliata puo' produrre corrispondenze errate fra residenza e utenza.
- Una via non collegata non blocca l'elaborazione, ma puo' far usare il fallback testuale e generare note.
