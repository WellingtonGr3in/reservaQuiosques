using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quiosques.Infrastructure.Persistence.Migrations
{
    // Dados iniciais (docs/05-modelagem-dados.md): quiosques A a F com nome, capacidade, valor e itens do protótipo.
    // As fotos apontam para arquivos servidos pelo frontend.
    public partial class SeedQuiosques : Migration
    {
        private const string UsageRules =
            "Horário de utilização: das 08:00 às 22:00. " +
            "O cancelamento sem custo pode ser feito com até 48 horas de antecedência. " +
            "Proibido o uso de som automotivo e caixas de som de alta potência.";

        private static readonly KioskSeed[] Kiosks =
        [
            new(
                "0199f5a0-0000-7000-8000-00000000000a", "Quiosque A", 30, 150.00m,
                "Estrutura coberta equipada com churrasqueira tradicional de alvenaria, grelha, pia com bancada de granito, " +
                "tomada 110v/220v, 6 mesas e 30 cadeiras de madeira. Ideal para confraternizações e aniversários.",
                ["Grelha e espetos", "Pia com água tratada", "Iluminação noturna", "Banheiros próximos"]),
            new(
                "0199f5a0-0000-7000-8000-00000000000b", "Quiosque B", 45, 200.00m,
                "Quiosque com capacidade para até 45 pessoas.",
                ["Churrasqueira dupla com grelha elevatória", "Tomadas 220v e ponto para som"]),
            new(
                "0199f5a0-0000-7000-8000-00000000000c", "Quiosque C", 20, 120.00m,
                "Quiosque com capacidade para até 20 pessoas.",
                ["Grelha tradicional e tábua de corte", "Área sombreada ao lado do bosque"]),
            new(
                "0199f5a0-0000-7000-8000-00000000000d", "Quiosque D", 50, 220.00m,
                "Quiosque com capacidade para até 50 pessoas.",
                ["Churrasqueira premium com espetos giratórios", "Sanitários privativos ao lado"]),
            new(
                "0199f5a0-0000-7000-8000-00000000000e", "Quiosque E", 25, 130.00m,
                "Quiosque com capacidade para até 25 pessoas.",
                ["Churrasqueira a carvão e grelha argentina", "Bebedouro refrigerado acoplado"]),
            new(
                "0199f5a0-0000-7000-8000-00000000000f", "Quiosque F", 35, 160.00m,
                "Quiosque com capacidade para até 35 pessoas.",
                ["Churrasqueira ampla e gaveteiro térmico", "Proximidade ao campo de futebol"]),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var kiosk in Kiosks)
            {
                var photoUrl = $"/imagens/quiosques/{kiosk.Slug}.jpg";

                migrationBuilder.InsertData(
                    table: "quiosque",
                    columns: ["id", "nome", "descricao", "capacidade", "valor_diaria", "foto_url", "regras_uso", "ativo"],
                    values: [kiosk.Id, kiosk.Name, kiosk.Description, kiosk.Capacity, kiosk.DailyRate, photoUrl, UsageRules, true]);

                migrationBuilder.InsertData(
                    table: "quiosque_foto",
                    columns: ["id", "quiosque_id", "url", "ordem"],
                    values: [DerivedId(kiosk.Id, 0x100), kiosk.Id, photoUrl, 1]);

                for (var i = 0; i < kiosk.Items.Length; i++)
                {
                    migrationBuilder.InsertData(
                        table: "quiosque_item",
                        columns: ["id", "quiosque_id", "descricao"],
                        values: [DerivedId(kiosk.Id, 0x200 + i), kiosk.Id, kiosk.Items[i]]);
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var kiosk in Kiosks)
            {
                migrationBuilder.DeleteData("quiosque_item", "quiosque_id", kiosk.Id);
                migrationBuilder.DeleteData("quiosque_foto", "quiosque_id", kiosk.Id);
                migrationBuilder.DeleteData("quiosque", "id", kiosk.Id);
            }
        }

        // Ids fixos, para que a migration gere sempre os mesmos registros.
        private static Guid DerivedId(Guid kioskId, int suffix)
        {
            var text = kioskId.ToString();
            return Guid.Parse($"{text[..19]}{suffix:x4}{text[23..]}");
        }

        private sealed record KioskSeed(string RawId, string Name, int Capacity, decimal DailyRate, string Description, string[] Items)
        {
            public Guid Id => Guid.Parse(RawId);

            public string Slug => Name.ToLowerInvariant().Replace(' ', '-');
        }
    }
}
