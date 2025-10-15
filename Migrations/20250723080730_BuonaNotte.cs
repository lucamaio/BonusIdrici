using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BonusIdrici2.Migrations
{
    /// <inheritdoc />
    public partial class BuonaNotte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Regione",
                table: "enti",
                newName: "regione");

            migrationBuilder.RenameColumn(
                name: "Provincia",
                table: "enti",
                newName: "provincia");

            migrationBuilder.RenameColumn(
                name: "Cap",
                table: "enti",
                newName: "cap");

            migrationBuilder.AlterColumn<DateTime>(
                name: "periodo_iniziale",
                table: "utenzeidriche",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<DateTime>(
                name: "periodo_finale",
                table: "utenzeidriche",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nome",
                table: "enti",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "istat",
                table: "enti",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "regione",
                table: "enti",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provincia",
                table: "enti",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodiceFiscale",
                table: "enti",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cap",
                table: "enti",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodiceFiscaleIntestatarioScheda",
                table: "dichiaranti",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<int>(
                name: "CodiceFamiglia",
                table: "dichiaranti",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.CreateTable(
                name: "domande",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    idAto = table.Column<string>(type: "longtext", nullable: false),
                    codice_bonus = table.Column<string>(type: "longtext", nullable: false),
                    esito_str = table.Column<string>(type: "longtext", nullable: false),
                    esito = table.Column<string>(type: "longtext", nullable: false),
                    idFornitura = table.Column<int>(type: "int", nullable: true),
                    codice_fiscale = table.Column<string>(type: "longtext", nullable: false),
                    numero_componenti = table.Column<string>(type: "longtext", nullable: false),
                    data_inzio = table.Column<string>(type: "longtext", nullable: false),
                    data_fine = table.Column<string>(type: "longtext", nullable: false),
                    nome_dichiarante = table.Column<string>(type: "longtext", nullable: true),
                    cognome_dichiarante = table.Column<string>(type: "longtext", nullable: true),
                    anno_validita = table.Column<string>(type: "longtext", nullable: true),
                    indirizzo_abitazione = table.Column<string>(type: "longtext", nullable: true),
                    numero_civico = table.Column<string>(type: "longtext", nullable: true),
                    istat = table.Column<string>(type: "longtext", nullable: true),
                    cap_abitazione = table.Column<string>(type: "longtext", nullable: true),
                    provincia_abitazione = table.Column<string>(type: "longtext", nullable: true),
                    presenza_pod = table.Column<string>(type: "longtext", nullable: true),
                    data_inizio_validita = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    data_fine_validita = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    id_ente = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_domande", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "domande");

            migrationBuilder.RenameColumn(
                name: "regione",
                table: "enti",
                newName: "Regione");

            migrationBuilder.RenameColumn(
                name: "provincia",
                table: "enti",
                newName: "Provincia");

            migrationBuilder.RenameColumn(
                name: "cap",
                table: "enti",
                newName: "Cap");

            migrationBuilder.AlterColumn<string>(
                name: "periodo_iniziale",
                table: "utenzeidriche",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "periodo_finale",
                table: "utenzeidriche",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Regione",
                table: "enti",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Provincia",
                table: "enti",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "nome",
                table: "enti",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "istat",
                table: "enti",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "Cap",
                table: "enti",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "CodiceFiscale",
                table: "enti",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<int>(
                name: "CodiceFiscaleIntestatarioScheda",
                table: "dichiaranti",
                type: "int",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "CodiceFamiglia",
                table: "dichiaranti",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
