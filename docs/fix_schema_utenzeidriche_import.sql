/*
  Riallinea la tabella utenzeidriche al modello EF usato dall'import CSV.

  Problema riscontrato:
  - il codice salva indirizzo_ubicazione fino a 255 caratteri;
  - il DB locale ha ancora indirizzo_ubicazione varchar(25);
  - nella stessa transazione vengono salvate utenze e snapshot, quindi un solo
    valore troppo lungo provoca rollback completo dell'import.
*/

ALTER TABLE utenzeidriche
  MODIFY COLUMN IdAcquedotto varchar(35) NOT NULL,
  MODIFY COLUMN matricola_contatore varchar(50) NOT NULL,
  MODIFY COLUMN indirizzo_ubicazione varchar(255) NOT NULL,
  MODIFY COLUMN scala_ubicazione varchar(50) NULL;
