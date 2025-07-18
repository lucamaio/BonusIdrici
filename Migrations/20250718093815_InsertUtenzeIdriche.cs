using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BonusIdrici2.Migrations
{
    /// <inheritdoc />
    public partial class InsertUtenzeIdriche : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Enti",
                table: "Enti");

            migrationBuilder.RenameTable(
                name: "Enti",
                newName: "enti");

            migrationBuilder.AddPrimaryKey(
                name: "PK_enti",
                table: "enti",
                column: "id");

            migrationBuilder.CreateTable(
                name: "fileuploads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nome = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    percorso = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    data_inizio = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    data_fine = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    data_caricamento = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    id_ente = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fileuploads", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "utenzeidriche",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    idAcquedotto = table.Column<string>(type: "longtext", nullable: false),
                    stato = table.Column<int>(type: "int", nullable: false),
                    periodo_iniziale = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    periodo_finale = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    matricola_contatore = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    indirizzo_ubicazione = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    numero_civico = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    sub_ubicazione = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    scala_ubicazione = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    piano = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    interno = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    tipo_utenza = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    cognome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    codice_fiscale = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    id_ente = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utenzeidriche", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fileuploads");

            migrationBuilder.DropTable(
                name: "utenzeidriche");

            migrationBuilder.DropPrimaryKey(
                name: "PK_enti",
                table: "enti");

            migrationBuilder.RenameTable(
                name: "enti",
                newName: "Enti");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Enti",
                table: "Enti",
                column: "id");
        }
    }
}
