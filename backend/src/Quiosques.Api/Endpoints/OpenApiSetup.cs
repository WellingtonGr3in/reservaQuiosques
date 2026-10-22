using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Quiosques.Api.Endpoints;

// Documentação dos endpoints (docs/06-api-endpoints.md): especificação OpenAPI em /openapi/v1.json
// e Swagger UI em /swagger. No Docker, o nginx repassa as duas rotas (http://localhost:8080/swagger).
public static class OpenApiSetup
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Reserva de Quiosques - API",
                    Version = "v1",
                    Description = "API do MVP de reserva de quiosques. Erros sempre no formato { \"message\": \"...\" }. "
                        + "Rotas de cliente e de administração exigem o token do login (botão Authorize).",
                };

                // Sem "servers": o Swagger UI usa o mesmo endereço em que foi aberto (localhost:5091 ou localhost:8080 pelo nginx).
                document.Servers = [];

                // Sem a tag padrão "Quiosques.Api", que aparecia como um grupo vazio; os grupos vêm de TagFor.
                document.Tags = null;

                // Botão "Authorize" do Swagger UI: o token JWT de POST /api/login ou POST /api/admin/login.
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Token devolvido por POST /api/login (cliente) ou POST /api/admin/login (administrador).",
                };
                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                var path = context.Description.RelativePath ?? "";
                operation.Tags = [new OpenApiTag { Name = TagFor(path) }];

                if (RequiresToken(path))
                {
                    operation.Security =
                    [
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = BearerScheme } }] = [],
                        },
                    ];
                }
                return Task.CompletedTask;
            });
        });

    public static WebApplication MapApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi(); // /openapi/v1.json
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "Reserva de Quiosques - API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Reserva de Quiosques - API";
        });
        return app;
    }

    // Agrupa as rotas por história de usuário na tela do Swagger.
    private static string TagFor(string path) => path switch
    {
        _ when path.StartsWith("api/admin/quiosques") || path.StartsWith("api/admin/imagens") => "Administração - Quiosques (HU07)",
        _ when path.StartsWith("api/admin") => "Administração - Reservas (HU06)",
        _ when path.StartsWith("api/quiosques") => "Quiosques (HU01)",
        _ when path.StartsWith("api/pagamentos") || path.StartsWith("api/dev") || path.EndsWith("/pagamento") && path.StartsWith("api/reservas") => "Pagamento Pix (HU03)",
        _ when path.StartsWith("api/reservas") => "Reservas sem conta (HU02, HU04)",
        _ => "Conta do cliente (HU05)",
    };

    private static bool RequiresToken(string path) =>
        (path.StartsWith("api/admin") && path != "api/admin/login")
        || path.StartsWith("api/minhas-reservas")
        || path.StartsWith("api/minha-conta");
}
