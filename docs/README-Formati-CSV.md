# Formati CSV richiesti

Questa cartella documenta l'ordine dei campi CSV letti dall'applicazione.

Il codice attuale legge i file con queste regole comuni:

- separatore campi: punto e virgola (`;`);
- la prima riga viene considerata intestazione e viene saltata;
- evitare valori che contengono `;`, perche' il parser usa `Split(';')`;
- le virgolette doppie vengono rimosse dai singoli valori;
- date accettate: `dd/MM/yyyy` oppure `yyyy-MM-dd`.

Regole specifiche:

- anagrafe e INPS restano tracciati posizionali;
- utenze idriche usa prima il nome delle intestazioni e ricade sugli indici storici solo se una colonna non viene trovata;
- nei CSV utenze Phiranha alcune colonne opzionali possono mancare o essere spostate, purche' siano presenti le intestazioni dei campi usati dal sistema.

File disponibili:

- [Formato CSV - Utenze Idriche](Formato-CSV-Utenze-Idriche.md)
- [Formato CSV - Anagrafe](Formato-CSV-Anagrafe.md)
- [Formato CSV - INPS Bonus Idrico](Formato-CSV-INPS-Bonus-Idrico.md)
