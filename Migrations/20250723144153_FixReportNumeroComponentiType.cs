using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonusIdrici2.Migrations
{
    /// <inheritdoc />
    public partial class FixDomandeNumeroComponentiType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_fine",
                table: "domande");

            migrationBuilder.DropColumn(
                name: "data_inzio",
                table: "domande");

            migrationBuilder.AlterColumn<int>(
                name: "numero_componenti",
                table: "domande",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AddColumn<DateTime>(
                name: "data_creazione",
                table: "domande",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "patitaIVA",
                table: "enti",
                type: "longtext",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_creazione",
                table: "domande");

            migrationBuilder.DropColumn(
                name: "patitaIVA",
                table: "enti");

            migrationBuilder.AlterColumn<string>(
                name: "numero_componenti",
                table: "domande",
                type: "longtext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "data_fine",
                table: "domande",
                type: "longtext",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "data_inzio",
                table: "domande",
                type: "longtext",
                nullable: false);
        }
    }
}
