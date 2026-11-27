using System.Security.Claims;
using System.Text;
using FinTrack.Infrastructure.Data;
using FinTrack.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FinTrack.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"] ?? string.Empty;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var db = context.HttpContext.RequestServices.GetRequiredService<FinTrackDbContext>();
                        var verification = context.HttpContext.RequestServices.GetRequiredService<EmailVerificationService>();
                        var claim = context.Principal?.FindFirstValue("pwd_version");
                        Guid? version = Guid.TryParseExact(claim, "N", out var parsed) ? parsed : null;
                        if (!Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ||
                            !await db.Users.AnyAsync(x => x.Id == id && x.PasswordVersion == version, context.HttpContext.RequestAborted) ||
                            !await verification.AcceptSessionAsync(id, touch: false, context.HttpContext.RequestAborted))
                            context.Fail("Session verification is required.");
                    }
                };

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }
}
