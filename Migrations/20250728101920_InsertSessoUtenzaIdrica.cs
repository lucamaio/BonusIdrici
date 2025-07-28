using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonusIdrici2.Migrations
{
    /// <inheritdoc />
    public partial class InsertSessoUtenzaIdrica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "periodo_iniziale",
                table: "utenzeidriche",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sesso",
                table: "utenzeidriche",
                type: "varchar(1)",
                maxLength: 1,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "regione",
                table: "enti",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "provincia",
                table: "enti",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "codice_fiscale",
                table: "enti",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldMaxLength: 16);

            migrationBuilder.AddColumn<bool>(
                name: "nostro",
                table: "enti",
                type: "tinyint(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CodiceFiscaleIntestatarioScheda",
                table: "dichiaranti",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 16);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sesso",
                table: "utenzeidriche");

            migrationBuilder.DropColumn(
                name: "nostro",
                table: "enti");

            migrationBuilder.AlterColumn<DateTime>(
                name: "periodo_iniziale",
                table: "utenzeidriche",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<string>(
                name: "regione",
                table: "enti",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provincia",
                table: "enti",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "codice_fiscale",
                table: "enti",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CodiceFiscaleIntestatarioScheda",
                table: "dichiaranti",
                type: "int",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldMaxLength: 16);
        }
    }
}
