using System.Security.Cryptography;
using System.Text;
using FinTrack.Application.DTOs.Auth;
using FinTrack.Application.Interfaces.Security;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Authentication;

public record RefreshCookie(string Value, DateTime ExpiresAt);

public class RefreshSessionService(FinTrackDbContext context, IJwtTokenGenerator jwt, TimeProvider clock, EmailVerificationService verification)
{
    public async Task<RefreshCookie> StartAsync(Guid userId, Guid? passwordVersion, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await context.RefreshSessions.Where(session => session.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
        var session = new RefreshSession { UserId = userId, PasswordVersion = passwordVersion, ExpiresAt = now.AddDays(30) };
        var cookie = Rotate(session);
        context.RefreshSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);
        return cookie;
    }

    public async Task<(AuthResponse Auth, RefreshCookie Cookie)?> RefreshAsync(
        string? value, CancellationToken cancellationToken)
    {
        var session = await FindAsync(value, cancellationToken);
        if (session is null || !await verification.AcceptSessionAsync(session.UserId, touch: true, cancellationToken))
        {
            return null;
        }

        var cookie = Rotate(session);
        await context.SaveChangesAsync(cancellationToken);
        return (new AuthResponse
        {
            UserId = session.UserId,
            Name = session.User.Name,
            Email = session.User.Email,
            Token = jwt.GenerateToken(session.User)
        }, cookie);
    }

    public async Task LogoutAsync(string? value, CancellationToken cancellationToken)
    {
        var session = await FindAsync(value, cancellationToken);
        if (session is null)
        {
            return;
        }

        context.RefreshSessions.Remove(session);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<RefreshSession?> FindAsync(string? value, CancellationToken cancellationToken)
    {
        if (value is null || value.Length != 97 || value[32] != '.' ||
            !Guid.TryParseExact(value[..32], "N", out var id))
        {
            return null;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var session = await context.RefreshSessions.Include(item => item.User)
            .SingleOrDefaultAsync(item => item.Id == id && item.ExpiresAt > now && item.PasswordVersion == item.User.PasswordVersion, cancellationToken);
        if (session is null)
        {
            return null;
        }

        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(session.TokenHash), Hash(value[33..])) ? session : null;
    }

    private static RefreshCookie Rotate(RefreshSession session)
    {
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        session.TokenHash = Convert.ToHexString(Hash(secret));
        return new($"{session.Id:N}.{secret}", session.ExpiresAt);
    }

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
