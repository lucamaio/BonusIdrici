using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BonusIdrici2.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Dichiaranti",
                columns: table => new
                {
                    IdDichiarante = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Cognome = table.Column<string>(type: "longtext", nullable: false),
                    Nome = table.Column<string>(type: "longtext", nullable: false),
                    CodiceFiscale = table.Column<string>(type: "longtext", nullable: false),
                    Sesso = table.Column<string>(type: "longtext", nullable: false),
                    DataNascita = table.Column<string>(type: "longtext", nullable: false),
                    ComuneNascita = table.Column<string>(type: "longtext", nullable: false),
                    IndirizzoResidenza = table.Column<string>(type: "longtext", nullable: false),
                    NumeroCivico = table.Column<string>(type: "longtext", nullable: false),
                    NomeEnte = table.Column<string>(type: "longtext", nullable: false),
                    NumeroComponenti = table.Column<string>(type: "longtext", nullable: false),
                    CodiceFamiglia = table.Column<string>(type: "longtext", nullable: false),
                    Parentela = table.Column<string>(type: "longtext", nullable: false),
                    CodiceFiscaleIntestatarioScheda = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dichiaranti", x => x.IdDichiarante);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Enti",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nome = table.Column<string>(type: "longtext", nullable: true),
                    istat = table.Column<string>(type: "longtext", nullable: true),
                    CodiceFiscale = table.Column<string>(type: "longtext", nullable: true),
                    Cap = table.Column<string>(type: "longtext", nullable: true),
                    Provincia = table.Column<string>(type: "longtext", nullable: true),
                    Regione = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enti", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dichiaranti");

            migrationBuilder.DropTable(
                name: "Enti");
        }
    }
}
