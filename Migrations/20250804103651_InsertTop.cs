using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BonusIdrici2.Migrations
{
    /// <inheritdoc />
    public partial class InsertTop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomeEnte",
                table: "dichiaranti");

            migrationBuilder.RenameColumn(
                name: "IdDichiarante",
                table: "dichiaranti",
                newName: "id");

            migrationBuilder.AddColumn<int>(
                name: "idEnte",
                table: "dichiaranti",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "toponomi",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    denominazione = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    normalizzazione = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    data_creazione = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    data_aggiornamento = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    id_ente = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_toponomi", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "toponomi");

            migrationBuilder.DropColumn(
                name: "idEnte",
                table: "dichiaranti");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "dichiaranti",
                newName: "IdDichiarante");

            migrationBuilder.AddColumn<string>(
                name: "NomeEnte",
                table: "dichiaranti",
                type: "varchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");
        }
    }
}
