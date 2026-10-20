using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Quiosques.Application.Accounts;

namespace Quiosques.Api.Auth;

public class JwtSettings
{
    public const string Section = "Jwt";

    // RNF04: a sessão expira em no máximo 60 minutos.
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(60);

    public string Issuer { get; set; } = "quiosques-api";
    public string Audience { get; set; } = "quiosques-web";

    // Chave HMAC com pelo menos 32 bytes. Em desenvolvimento vem do appsettings.Development.json.
    public string Key { get; set; } = "";

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));
}

public record AccessToken(string Token, DateTimeOffset ExpiresAt);

// A validade do token é uma questão técnica de sessão, por isso usa o relógio real (TimeProvider),
// o mesmo que o middleware de autenticação usa para validar, e não o IClock das regras de negócio.
public class JwtTokenIssuer(IOptions<JwtSettings> options, TimeProvider timeProvider)
{
    public const string CustomerIdClaim = "customer_id";

    public AccessToken Issue(AuthenticatedAccount account)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now + JwtSettings.Lifetime;

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, account.AccountId.ToString()),
                new Claim(CustomerIdClaim, account.CustomerId.ToString()),
            ],
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256));

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    // RF11: token do administrador, com o papel "admin" e sem cliente.
    public AccessToken IssueForAdministrator(Application.Admin.AuthenticatedAdministrator administrator)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now + JwtSettings.Lifetime;

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, administrator.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, administrator.Name),
                new Claim(RoleClaim, AdministratorRole),
            ],
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256));

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public const string RoleClaim = "role";
    public const string AdministratorRole = "admin";

    public static Guid CustomerIdOf(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(CustomerIdClaim) ?? throw new InvalidOperationException("Token sem customer_id."));
}

public static class AuthSetup
{
    public const string CustomerPolicy = "cliente";
    public const string AdministratorPolicy = "administrador";

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.Section).Get<JwtSettings>() ?? new JwtSettings();
        if (Encoding.UTF8.GetByteCount(settings.Key) < 32)
        {
            throw new InvalidOperationException("Configure 'Jwt:Key' com pelo menos 32 bytes.");
        }

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.Section));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<JwtTokenIssuer>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = settings.SigningKey,
                    ClockSkew = TimeSpan.Zero,
                };
            });
        // Clientes e administradores usam o mesmo tipo de token, mas um não acessa a área do outro.
        services.AddAuthorizationBuilder()
            .AddPolicy(CustomerPolicy, policy => policy.RequireClaim(JwtTokenIssuer.CustomerIdClaim))
            .AddPolicy(AdministratorPolicy, policy => policy.RequireClaim(JwtTokenIssuer.RoleClaim, JwtTokenIssuer.AdministratorRole));

        return services;
    }
}
