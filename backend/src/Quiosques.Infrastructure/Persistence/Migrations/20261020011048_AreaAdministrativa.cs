using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quiosques.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AreaAdministrativa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "metodo",
                table: "pagamento",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PIX");

            migrationBuilder.CreateTable(
                name: "administrador",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_administrador", x => x.id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_pagamento_metodo",
                table: "pagamento",
                sql: "metodo IN ('PIX', 'ADMINISTRACAO')");

            migrationBuilder.CreateIndex(
                name: "ux_administrador_email",
                table: "administrador",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "administrador");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pagamento_metodo",
                table: "pagamento");

            migrationBuilder.DropColumn(
                name: "metodo",
                table: "pagamento");
        }
    }
}
