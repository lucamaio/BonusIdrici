CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
CREATE TABLE `Atti` (
    `id` int NOT NULL AUTO_INCREMENT,
    `OriginalCsvId` int NOT NULL,
    `codBonusIdrico` bigint NOT NULL,
    `Anno` int NOT NULL,
    `DataInizio` datetime(6) NOT NULL,
    `DataFine` datetime(6) NOT NULL,
    `PRESENZA_POD` tinyint(1) NOT NULL,
    PRIMARY KEY (`id`)
);

CREATE TABLE `Dichiaranti` (
    `IdDichiarante` int NOT NULL AUTO_INCREMENT,
    `Nome` longtext NOT NULL,
    `Cognome` longtext NOT NULL,
    `CfDichiarante` longtext NOT NULL,
    `IndirizzoAbitazione` longtext NOT NULL,
    `NumeroCivico` longtext NOT NULL,
    `Cap` longtext NOT NULL,
    `Istat` longtext NOT NULL,
    `ProvinciaAbitazione` longtext NOT NULL,
    `CfMembri` longtext NOT NULL,
    PRIMARY KEY (`IdDichiarante`)
);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20250703162117_InitialDatabaseSchema', '9.0.6');

COMMIT;

