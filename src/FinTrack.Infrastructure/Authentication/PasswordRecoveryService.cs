using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using FinTrack.Application.Interfaces.Security;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinTrack.Infrastructure.Authentication;

public sealed class PasswordRecoveryService(
    FinTrackDbContext db, TimeProvider clock, EmailVerificationService mail,
    IPasswordHasher passwords, IOptions<JwtSettings> jwt, ILogger<PasswordRecoveryService> logger)
{
    public async Task<Guid> RequestAsync(string email, CancellationToken ct)
    {
        var challenge = Guid.NewGuid();
        if (!mail.Enabled) return challenge;

        var now = clock.GetUtcNow().UtcDateTime;
        var code = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var hash = Hash(challenge, code);
        var user = db.Users.Where(x => x.Email == email &&
            (x.PasswordResetSentAt == null || x.PasswordResetSentAt <= now.AddMinutes(-1)));
        if (await user.ExecuteUpdateAsync(set => set.SetProperty(x => x.PasswordResetId, challenge)
            .SetProperty(x => x.PasswordResetHash, hash)
            .SetProperty(x => x.PasswordResetSentAt, now)
            .SetProperty(x => x.PasswordResetAttempts, 0), ct) == 0)
            return challenge;

        try
        {
            await mail.SendEmailAsync(email, "FinTrack | Redefinição de senha",
                $"Seu código para redefinir a senha do FinTrack é {code}. Ele expira em 10 minutos. Se você não solicitou a redefinição, ignore esta mensagem.", ct);
        }
        catch (Exception error) when (error is SmtpException or HttpRequestException or OperationCanceledException)
        {
            await db.Users.Where(x => x.PasswordResetId == challenge)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.PasswordResetHash, (string?)null), CancellationToken.None);
            if (ct.IsCancellationRequested) throw;
            logger.LogWarning("Unable to deliver a password recovery email.");
        }

        return challenge;
    }

    public async Task<bool> ResetAsync(Guid challenge, string code, string password, CancellationToken ct)
    {
        if (!mail.Enabled) return false;
        var now = clock.GetUtcNow().UtcDateTime;
        var hash = Hash(challenge, code);
        var eligible = db.Users.Where(x => x.PasswordResetId == challenge && x.PasswordResetHash != null &&
            x.PasswordResetSentAt > now.AddMinutes(-10) && x.PasswordResetAttempts < 5);
        var match = eligible.Where(x => x.PasswordResetHash == hash);
        var id = await match.Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (id is null)
        {
            await eligible.ExecuteUpdateAsync(set => set.SetProperty(x => x.PasswordResetAttempts, x => x.PasswordResetAttempts + 1), ct);
            return false;
        }

        var passwordHash = passwords.Hash(password);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (await match.ExecuteUpdateAsync(set => set.SetProperty(x => x.PasswordHash, passwordHash)
            .SetProperty(x => x.PasswordVersion, Guid.NewGuid())
            .SetProperty(x => x.PasswordResetHash, (string?)null)
            .SetProperty(x => x.PasswordResetId, (Guid?)null)
            .SetProperty(x => x.UpdatedAt, now), ct) == 0)
            return false;

        await db.EmailVerifications.Where(x => x.UserId == id).ExecuteUpdateAsync(set => set
            .SetProperty(x => x.CodeHash, (string?)null)
            .SetProperty(x => x.VerifiedAt, now)
            .SetProperty(x => x.LastActivityAt, now), ct);
        await db.RefreshSessions.Where(x => x.UserId == id).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private string Hash(Guid challenge, string code) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(jwt.Value.Secret),
            Encoding.UTF8.GetBytes($"password-recovery:{challenge:N}:{code}")));
}
