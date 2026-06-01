# Formati CSV richiesti

Questa cartella documenta l'ordine dei campi CSV letti dall'applicazione.

Il codice attuale legge i file con queste regole comuni:

- separatore campi: punto e virgola (`;`);
- la prima riga viene considerata intestazione e viene saltata;
- l'ordine dei campi e' vincolante: il codice usa la posizione della colonna, non il nome dell'intestazione;
- evitare valori che contengono `;`, perche' il parser usa `Split(';')`;
- le virgolette doppie vengono rimosse dai singoli valori;
- date accettate: `dd/MM/yyyy` oppure `yyyy-MM-dd`.

File disponibili:

- [Formato CSV - Utenze Idriche](Formato-CSV-Utenze-Idriche.md)
- [Formato CSV - Anagrafe](Formato-CSV-Anagrafe.md)
- [Formato CSV - INPS Bonus Idrico](Formato-CSV-INPS-Bonus-Idrico.md)

