using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quiosques.Infrastructure.Persistence.Migrations
{
    // Troca as descrições mínimas dos quiosques B a F (SeedQuiosques) pelas usadas no frontend.
    public partial class AtualizaDescricoesQuiosques : Migration
    {
        private static readonly (string Id, string Description, string Placeholder)[] Descriptions =
        [
            ("0199f5a0-0000-7000-8000-00000000000b",
                "Espaçoso quiosque estilo pavilhão com ampla estrutura aberta, bancadas duplas, churrasqueira em inox e " +
                "sistema de tomadas reforçadas para som ambiente e iluminação decorativa.",
                "Quiosque com capacidade para até 45 pessoas."),
            ("0199f5a0-0000-7000-8000-00000000000c",
                "Quiosque intimista e aconchegante cercado pela vegetação nativa do bosque. Equipado com churrasqueira " +
                "giratória compacta, mesa de apoio e bancada de alvenaria.",
                "Quiosque com capacidade para até 20 pessoas."),
            ("0199f5a0-0000-7000-8000-00000000000d",
                "Grande pavilhão com vigas em madeira aparente, espetos giratórios elétricos, bancada em granito nobre e " +
                "sanitários exclusivos para seus convidados.",
                "Quiosque com capacidade para até 50 pessoas."),
            ("0199f5a0-0000-7000-8000-00000000000e",
                "Quiosque tranquilo com telhado cerâmico tradicional, pia com cuba dupla e bebedouro refrigerado, ótimo " +
                "para reuniões familiares em clima calmo.",
                "Quiosque com capacidade para até 25 pessoas."),
            ("0199f5a0-0000-7000-8000-00000000000f",
                "Quiosque de médio porte situado ao lado do campo de futebol society e quadra de beach tennis. Possui " +
                "gaveteiro térmico em inox e balcão de serviço.",
                "Quiosque com capacidade para até 35 pessoas."),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (id, description, _) in Descriptions)
            {
                migrationBuilder.UpdateData("quiosque", "id", Guid.Parse(id), "descricao", description);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (id, _, placeholder) in Descriptions)
            {
                migrationBuilder.UpdateData("quiosque", "id", Guid.Parse(id), "descricao", placeholder);
            }
        }
    }
}
