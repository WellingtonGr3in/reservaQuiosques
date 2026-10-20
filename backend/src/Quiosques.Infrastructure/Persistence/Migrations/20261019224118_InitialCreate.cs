using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quiosques.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cliente",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cpf = table.Column<string>(type: "char(11)", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cliente", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quiosque",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    capacidade = table.Column<int>(type: "integer", nullable: false),
                    valor_diaria = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    foto_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    regras_uso = table.Column<string>(type: "text", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quiosque", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "conta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_login = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conta", x => x.id);
                    table.ForeignKey(
                        name: "FK_conta_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quiosque_foto",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quiosque_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quiosque_foto", x => x.id);
                    table.ForeignKey(
                        name: "FK_quiosque_foto_quiosque_quiosque_id",
                        column: x => x.quiosque_id,
                        principalTable: "quiosque",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quiosque_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quiosque_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quiosque_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_quiosque_item_quiosque_quiosque_id",
                        column: x => x.quiosque_id,
                        principalTable: "quiosque",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reserva",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quiosque_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_reserva = table.Column<DateOnly>(type: "date", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expira_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reserva", x => x.id);
                    table.CheckConstraint("ck_reserva_status", "status IN ('PENDENTE_PAGAMENTO', 'CONFIRMADA', 'EXPIRADA', 'CANCELADA')");
                    table.ForeignKey(
                        name: "FK_reserva_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reserva_quiosque_quiosque_id",
                        column: x => x.quiosque_id,
                        principalTable: "quiosque",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reserva_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transacao_gateway_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pago_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagamento", x => x.id);
                    table.CheckConstraint("ck_pagamento_status", "status IN ('PENDENTE', 'APROVADO', 'REEMBOLSADO')");
                    table.ForeignKey(
                        name: "FK_pagamento_reserva_reserva_id",
                        column: x => x.reserva_id,
                        principalTable: "reserva",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_cliente_cpf",
                table: "cliente",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_conta_cliente_id",
                table: "conta",
                column: "cliente_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_conta_email_login",
                table: "conta",
                column: "email_login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagamento_reserva_id",
                table: "pagamento",
                column: "reserva_id");

            migrationBuilder.CreateIndex(
                name: "ux_pagamento_transacao_gateway_id",
                table: "pagamento",
                column: "transacao_gateway_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quiosque_foto_quiosque_id",
                table: "quiosque_foto",
                column: "quiosque_id");

            migrationBuilder.CreateIndex(
                name: "IX_quiosque_item_quiosque_id",
                table: "quiosque_item",
                column: "quiosque_id");

            migrationBuilder.CreateIndex(
                name: "IX_reserva_cliente_id",
                table: "reserva",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ux_reserva_codigo",
                table: "reserva",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_reserva_quiosque_data_ativa",
                table: "reserva",
                columns: new[] { "quiosque_id", "data_reserva" },
                unique: true,
                filter: "status IN ('PENDENTE_PAGAMENTO', 'CONFIRMADA')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conta");

            migrationBuilder.DropTable(
                name: "pagamento");

            migrationBuilder.DropTable(
                name: "quiosque_foto");

            migrationBuilder.DropTable(
                name: "quiosque_item");

            migrationBuilder.DropTable(
                name: "reserva");

            migrationBuilder.DropTable(
                name: "cliente");

            migrationBuilder.DropTable(
                name: "quiosque");
        }
    }
}
