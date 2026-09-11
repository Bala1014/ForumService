using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Racinglazing.Forum.Api.Identity;

/// <summary>
/// Wires JWT bearer authentication when an authority/issuer is configured
/// (production, once UserService/IdP exist). When nothing is configured, auth
/// is skipped and the dev-header fallback in <see cref="CurrentUser"/> applies —
/// so the service runs locally today without a real token provider.
/// </summary>
public static class AuthenticationSetup
{
    public static IServiceCollection AddForumAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authority = configuration["ForumAuth:Authority"];
        var issuer = configuration["ForumAuth:Issuer"];
        var audience = configuration["ForumAuth:Audience"];

        if (string.IsNullOrWhiteSpace(authority) && string.IsNullOrWhiteSpace(issuer))
            return services; // No IdP configured — dev-header mode.

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                if (!string.IsNullOrWhiteSpace(authority)) options.Authority = authority;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                    ValidIssuer = issuer,
                    ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            });
        services.AddAuthorization();

        return services;
    }
}
