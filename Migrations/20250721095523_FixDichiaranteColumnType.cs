using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BonusIdrici2.Migrations
{
    /// <inheritdoc />
    public partial class FixDichiaranteColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fileuploads");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Dichiaranti",
                table: "Dichiaranti");

            migrationBuilder.RenameTable(
                name: "Dichiaranti",
                newName: "dichiaranti");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "dichiaranti",
                newName: "nome");

            migrationBuilder.RenameColumn(
                name: "DataNascita",
                table: "dichiaranti",
                newName: "dataNascita");

            migrationBuilder.RenameColumn(
                name: "Cognome",
                table: "dichiaranti",
                newName: "cognome");

            migrationBuilder.RenameColumn(
                name: "CodiceFiscale",
                table: "dichiaranti",
                newName: "codiceFiscale");

            migrationBuilder.AlterColumn<string>(
                name: "periodo_iniziale",
                table: "utenzeidriche",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<string>(
                name: "periodo_finale",
                table: "utenzeidriche",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Sesso",
                table: "dichiaranti",
                type: "varchar(1)",
                maxLength: 1,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "Parentela",
                table: "dichiaranti",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroComponenti",
                table: "dichiaranti",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCivico",
                table: "dichiaranti",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "NomeEnte",
                table: "dichiaranti",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "nome",
                table: "dichiaranti",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "IndirizzoResidenza",
                table: "dichiaranti",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "ComuneNascita",
                table: "dichiaranti",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "cognome",
                table: "dichiaranti",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "codiceFiscale",
                table: "dichiaranti",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AddPrimaryKey(
                name: "PK_dichiaranti",
                table: "dichiaranti",
                column: "IdDichiarante");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_dichiaranti",
                table: "dichiaranti");

            migrationBuilder.RenameTable(
                name: "dichiaranti",
                newName: "Dichiaranti");

            migrationBuilder.RenameColumn(
                name: "nome",
                table: "Dichiaranti",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "dataNascita",
                table: "Dichiaranti",
                newName: "DataNascita");

            migrationBuilder.RenameColumn(
                name: "cognome",
                table: "Dichiaranti",
                newName: "Cognome");

            migrationBuilder.RenameColumn(
                name: "codiceFiscale",
                table: "Dichiaranti",
                newName: "CodiceFiscale");

            migrationBuilder.AlterColumn<DateTime>(
                name: "periodo_iniziale",
                table: "utenzeidriche",
                type: "datetime(6)",
                nullable: false,
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
                name: "Nome",
                table: "Dichiaranti",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Cognome",
                table: "Dichiaranti",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "CodiceFiscale",
                table: "Dichiaranti",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "Sesso",
                table: "Dichiaranti",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldMaxLength: 1);

            migrationBuilder.AlterColumn<string>(
                name: "Parentela",
                table: "Dichiaranti",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroComponenti",
                table: "Dichiaranti",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCivico",
                table: "Dichiaranti",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "NomeEnte",
                table: "Dichiaranti",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "IndirizzoResidenza",
                table: "Dichiaranti",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "ComuneNascita",
                table: "Dichiaranti",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Dichiaranti",
                table: "Dichiaranti",
                column: "IdDichiarante");

            migrationBuilder.CreateTable(
                name: "fileuploads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    data_caricamento = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    data_fine = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    data_inizio = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    id_ente = table.Column<int>(type: "int", nullable: false),
                    nome = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    percorso = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fileuploads", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }
    }
}
