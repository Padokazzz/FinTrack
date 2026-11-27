using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FinTrack.Application.DTOs.Auth;
using FinTrack.Application.Interfaces.Security;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FinTrack.Infrastructure.Authentication;

public sealed class EmailVerificationSettings
{
    public bool Enabled { get; set; } = true;
    public int InactiveDays { get; set; } = 14;
    public int VerifyEveryDays { get; set; } = 30;
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string From { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string BrevoApiKey { get; set; } = "";
    public string? PickupDirectory { get; set; }
}

public sealed class EmailVerificationService(
    FinTrackDbContext db,
    TimeProvider clock,
    IOptions<EmailVerificationSettings> options,
    IOptions<JwtSettings> jwtOptions,
    IJwtTokenGenerator jwt,
    HttpClient http)
{
    private readonly EmailVerificationSettings settings = options.Value;
    public bool Enabled => settings.Enabled;

    public async Task<bool> AcceptSessionAsync(Guid userId, bool touch, CancellationToken ct)
    {
        if (!Enabled) return true;
        var now = clock.GetUtcNow().UtcDateTime;
        var periodic = now.AddDays(-settings.VerifyEveryDays);
        var inactive = now.AddDays(-settings.InactiveDays);
        var eligible = db.EmailVerifications.Where(x =>
            x.UserId == userId && x.VerifiedAt > periodic && x.LastActivityAt > inactive);
        if (!touch) return await eligible.AnyAsync(ct);
        return await eligible.ExecuteUpdateAsync(set => set.SetProperty(
            x => x.LastActivityAt, x => x.LastActivityAt > now ? x.LastActivityAt : now), ct) == 1;
    }

    public async Task<Guid?> IssueAsync(AuthResponse user, CancellationToken ct)
    {
        var challenge = Guid.NewGuid();
        var code = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var hash = Hash(challenge, code);
        var now = clock.GetUtcNow().UtcDateTime;
        var updated = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "EmailVerifications" ("UserId", "ChallengeId", "CodeHash", "SentAt", "Attempts")
            VALUES ({user.UserId}, {challenge}, {hash}, {now}, 0)
            ON CONFLICT ("UserId") DO UPDATE SET
                "ChallengeId" = EXCLUDED."ChallengeId", "CodeHash" = EXCLUDED."CodeHash",
                "SentAt" = EXCLUDED."SentAt", "Attempts" = 0
            WHERE "EmailVerifications"."SentAt" <= {now.AddMinutes(-1)}
            """, ct);
        if (updated == 0) return null;

        try
        {
            const string subject = "FinTrack | Código de segurança";
            var body = $"Seu código de segurança FinTrack é {code}. Ele expira em 10 minutos. Não compartilhe esse código. Se você não o solicitou, ignore esta mensagem.";
            await SendEmailAsync(user.Email, subject, body, ct);
        }
        catch
        {
            await db.EmailVerifications.Where(x => x.ChallengeId == challenge)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.SentAt, now.AddMinutes(-1))
                    .SetProperty(x => x.CodeHash, (string?)null), CancellationToken.None);
            throw;
        }

        return challenge;
    }

    public async Task<AuthResponse?> ConfirmAsync(Guid challenge, string code, CancellationToken ct)
    {
        if (!Enabled) return null;
        var now = clock.GetUtcNow().UtcDateTime;
        var eligible = db.EmailVerifications.Where(x => x.ChallengeId == challenge &&
            x.CodeHash != null && x.SentAt > now.AddMinutes(-10) && x.Attempts < 5);
        var hash = Hash(challenge, code);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var confirmed = await eligible.Where(x => x.CodeHash == hash).ExecuteUpdateAsync(set => set
            .SetProperty(x => x.CodeHash, (string?)null)
            .SetProperty(x => x.VerifiedAt, now)
            .SetProperty(x => x.LastActivityAt, now), ct);
        if (confirmed == 0)
        {
            await eligible.ExecuteUpdateAsync(set => set.SetProperty(x => x.Attempts, x => x.Attempts + 1), ct);
            await transaction.CommitAsync(ct);
            return null;
        }

        var user = await db.Users.SingleAsync(x =>
            db.EmailVerifications.Any(state => state.ChallengeId == challenge && state.UserId == x.Id), ct);
        await db.RefreshSessions.Where(x => x.UserId == user.Id).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        return new AuthResponse { UserId = user.Id, PasswordVersion = user.PasswordVersion, Email = user.Email, Name = user.Name, Token = jwt.GenerateToken(user) };
    }

    internal async Task SendEmailAsync(string email, string subject, string body, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(settings.BrevoApiKey))
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", settings.BrevoApiKey);
            request.Content = JsonContent.Create(new
            {
                sender = new { email = settings.From },
                to = new[] { new { email = email } },
                subject,
                textContent = body
            });
            using var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return;
        }

        using var message = new MailMessage(settings.From, email, subject, body);
        using var client = string.IsNullOrEmpty(settings.PickupDirectory)
            ? new SmtpClient(settings.Host, settings.Port) { EnableSsl = true }
            : new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = settings.PickupDirectory
            };
        if (!string.IsNullOrEmpty(settings.Username))
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        await client.SendMailAsync(message, timeout.Token);
    }

    private string Hash(Guid challenge, string code) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(jwtOptions.Value.Secret),
            Encoding.UTF8.GetBytes($"email-verification:{challenge:N}:{code}")));
}
