using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quiosques.Infrastructure.Persistence.Migrations
{
    // Troca os nomes "Quiosque A" a "Quiosque F" do seed por nomes dos locais do condomínio.
    public partial class RenomeiaQuiosques : Migration
    {
        private static readonly (string Id, string Name, string OldName)[] Names =
        [
            ("0199f5a0-0000-7000-8000-00000000000a", "Quiosque das Mangueiras", "Quiosque A"),
            ("0199f5a0-0000-7000-8000-00000000000b", "Pavilhão Central", "Quiosque B"),
            ("0199f5a0-0000-7000-8000-00000000000c", "Quiosque dos Ipês", "Quiosque C"),
            ("0199f5a0-0000-7000-8000-00000000000d", "Quiosque das Palmeiras", "Quiosque D"),
            ("0199f5a0-0000-7000-8000-00000000000e", "Quiosque Jardim dos Pássaros", "Quiosque E"),
            ("0199f5a0-0000-7000-8000-00000000000f", "Quiosque da Quadra", "Quiosque F"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (id, name, _) in Names)
            {
                migrationBuilder.UpdateData("quiosque", "id", Guid.Parse(id), "nome", name);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (id, _, oldName) in Names)
            {
                migrationBuilder.UpdateData("quiosque", "id", Guid.Parse(id), "nome", oldName);
            }
        }
    }
}
