/*
    Script MySQL per allineare le tabelle usate dalla gestione Vie Ente.
    Database dell'applicazione: bonus

    Nota:
    - Lo script crea le tabelle se non esistono.
    - Se le tabelle esistono gia' nella vecchia forma, esegue gli ALTER necessari.
    - Le vecchie colonne non piu' usate dal model EF vengono lasciate nel DB per prudenza.
*/

CREATE TABLE IF NOT EXISTS `indirizzi_normalizzati` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `id_ente` INT NOT NULL,
    `denominazione_normalizzata` VARCHAR(255) NOT NULL,
    `data_creazione` DATETIME NULL,
    `data_aggiornamento` DATETIME NULL,
    `id_user` INT NOT NULL DEFAULT 1,
    `attivo` TINYINT(1) NOT NULL DEFAULT 1,
    `note` LONGTEXT NULL,
    PRIMARY KEY (`id`)
);

CREATE TABLE IF NOT EXISTS `vie_ente` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `id_ente` INT NOT NULL,
    `denominazione_originale` VARCHAR(255) NOT NULL,
    `denominazione_pulita` VARCHAR(255) NOT NULL,
    `denominazione_normalizzata_proposta` VARCHAR(255) NULL,
    `tipologia_via` VARCHAR(50) NOT NULL,
    `civico_estratto` VARCHAR(50) NULL,
    `fonte` VARCHAR(50) NOT NULL DEFAULT 'UTENZE',
    `occorrenze` INT NOT NULL DEFAULT 1,
    `stato` VARCHAR(50) NOT NULL DEFAULT 'DA_ANALIZZARE',
    `id_indirizzo_normalizzato` INT NULL,
    `data_creazione` DATETIME NULL,
    `data_aggiornamento` DATETIME NULL,
    `id_user` INT NOT NULL DEFAULT 1,
    `note` LONGTEXT NULL,
    PRIMARY KEY (`id`)
);

SET @db_name = DATABASE();

SET @sql = (
    SELECT IF(
        EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'indirizzi_normalizzati'
              AND COLUMN_NAME = 'denominazione'
        )
        AND NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'indirizzi_normalizzati'
              AND COLUMN_NAME = 'denominazione_normalizzata'
        ),
        'ALTER TABLE `indirizzi_normalizzati` CHANGE COLUMN `denominazione` `denominazione_normalizzata` VARCHAR(255) NOT NULL',
        'SELECT ''indirizzi_normalizzati.denominazione_normalizzata gia presente o denominazione assente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND COLUMN_NAME = 'denominazione'
        )
        AND NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND COLUMN_NAME = 'denominazione_originale'
        ),
        'ALTER TABLE `vie_ente` CHANGE COLUMN `denominazione` `denominazione_originale` VARCHAR(255) NOT NULL',
        'SELECT ''vie_ente.denominazione_originale gia presente o denominazione assente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND COLUMN_NAME = 'tipo_via'
        )
        AND NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND COLUMN_NAME = 'tipologia_via'
        ),
        'ALTER TABLE `vie_ente` CHANGE COLUMN `tipo_via` `tipologia_via` VARCHAR(50) NOT NULL',
        'SELECT ''vie_ente.tipologia_via gia presente o tipo_via assente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'indirizzi_normalizzati'
              AND COLUMN_NAME = 'id_ente'
        ),
        'ALTER TABLE `indirizzi_normalizzati` ADD COLUMN `id_ente` INT NOT NULL DEFAULT 0 AFTER `id`',
        'SELECT ''indirizzi_normalizzati.id_ente gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'indirizzi_normalizzati'
              AND COLUMN_NAME = 'id_user'
        ),
        'ALTER TABLE `indirizzi_normalizzati` ADD COLUMN `id_user` INT NOT NULL DEFAULT 1 AFTER `data_aggiornamento`',
        'SELECT ''indirizzi_normalizzati.id_user gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'indirizzi_normalizzati'
              AND COLUMN_NAME = 'attivo'
        ),
        'ALTER TABLE `indirizzi_normalizzati` ADD COLUMN `attivo` TINYINT(1) NOT NULL DEFAULT 1 AFTER `id_user`',
        'SELECT ''indirizzi_normalizzati.attivo gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'indirizzi_normalizzati'
              AND COLUMN_NAME = 'note'
        ),
        'ALTER TABLE `indirizzi_normalizzati` ADD COLUMN `note` LONGTEXT NULL AFTER `attivo`',
        'SELECT ''indirizzi_normalizzati.note gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND COLUMN_NAME = 'denominazione_pulita'
        ),
        'ALTER TABLE `vie_ente` ADD COLUMN `denominazione_pulita` VARCHAR(255) NULL AFTER `denominazione_originale`',
        'SELECT ''vie_ente.denominazione_pulita gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

UPDATE `vie_ente`
SET `denominazione_pulita` = `denominazione_originale`
WHERE `denominazione_pulita` IS NULL OR TRIM(`denominazione_pulita`) = '';

ALTER TABLE `vie_ente` MODIFY COLUMN `denominazione_pulita` VARCHAR(255) NOT NULL;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND COLUMN_NAME = 'denominazione_normalizzata_proposta'
        ),
        'ALTER TABLE `vie_ente` ADD COLUMN `denominazione_normalizzata_proposta` VARCHAR(255) NULL AFTER `denominazione_pulita`',
        'SELECT ''vie_ente.denominazione_normalizzata_proposta gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND COLUMN_NAME = 'civico_estratto'
        ),
        'ALTER TABLE `vie_ente` ADD COLUMN `civico_estratto` VARCHAR(50) NULL AFTER `tipologia_via`',
        'SELECT ''vie_ente.civico_estratto gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND COLUMN_NAME = 'fonte'
        ),
        'ALTER TABLE `vie_ente` ADD COLUMN `fonte` VARCHAR(50) NOT NULL DEFAULT ''UTENZE'' AFTER `civico_estratto`',
        'SELECT ''vie_ente.fonte gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND COLUMN_NAME = 'occorrenze'
        ),
        'ALTER TABLE `vie_ente` ADD COLUMN `occorrenze` INT NOT NULL DEFAULT 1 AFTER `fonte`',
        'SELECT ''vie_ente.occorrenze gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND COLUMN_NAME = 'stato'
        ),
        'ALTER TABLE `vie_ente` ADD COLUMN `stato` VARCHAR(50) NOT NULL DEFAULT ''DA_ANALIZZARE'' AFTER `occorrenze`',
        'SELECT ''vie_ente.stato gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

ALTER TABLE `vie_ente` MODIFY COLUMN `id_indirizzo_normalizzato` INT NULL;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND COLUMN_NAME = 'id_user'
        ),
        'ALTER TABLE `vie_ente` ADD COLUMN `id_user` INT NOT NULL DEFAULT 1 AFTER `data_aggiornamento`',
        'SELECT ''vie_ente.id_user gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND COLUMN_NAME = 'note'
        ),
        'ALTER TABLE `vie_ente` ADD COLUMN `note` LONGTEXT NULL AFTER `id_user`',
        'SELECT ''vie_ente.note gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND INDEX_NAME = 'ix_vie_ente_originale_fonte'
        ),
        'CREATE INDEX `ix_vie_ente_originale_fonte` ON `vie_ente` (`id_ente`, `denominazione_originale`, `fonte`)',
        'SELECT ''ix_vie_ente_originale_fonte gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND INDEX_NAME = 'ix_vie_ente_pulita'
        ),
        'CREATE INDEX `ix_vie_ente_pulita` ON `vie_ente` (`id_ente`, `denominazione_pulita`)',
        'SELECT ''ix_vie_ente_pulita gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND INDEX_NAME = 'ix_vie_ente_stato'
        ),
        'CREATE INDEX `ix_vie_ente_stato` ON `vie_ente` (`id_ente`, `stato`)',
        'SELECT ''ix_vie_ente_stato gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND INDEX_NAME = 'ix_vie_ente_indirizzo_normalizzato'
        ),
        'CREATE INDEX `ix_vie_ente_indirizzo_normalizzato` ON `vie_ente` (`id_indirizzo_normalizzato`)',
        'SELECT ''ix_vie_ente_indirizzo_normalizzato gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = @db_name
              AND TABLE_NAME = 'indirizzi_normalizzati'
              AND INDEX_NAME = 'ux_indirizzi_normalizzati_ente_denominazione'
        ),
        'CREATE UNIQUE INDEX `ux_indirizzi_normalizzati_ente_denominazione` ON `indirizzi_normalizzati` (`id_ente`, `denominazione_normalizzata`)',
        'SELECT ''ux_indirizzi_normalizzati_ente_denominazione gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
            WHERE CONSTRAINT_SCHEMA = @db_name
              AND TABLE_NAME = 'vie_ente'
              AND CONSTRAINT_NAME = 'fk_vie_ente_indirizzi_normalizzati'
        ),
        'ALTER TABLE `vie_ente` ADD CONSTRAINT `fk_vie_ente_indirizzi_normalizzati` FOREIGN KEY (`id_indirizzo_normalizzato`) REFERENCES `indirizzi_normalizzati` (`id`) ON DELETE SET NULL',
        'SELECT ''fk_vie_ente_indirizzi_normalizzati gia presente''')
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
