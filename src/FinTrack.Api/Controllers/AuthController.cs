using System.Net.Mail;
using System.ComponentModel.DataAnnotations;
using FinTrack.Application.DTOs.Auth;
using FinTrack.Infrastructure.Privacy;
using Microsoft.AspNetCore.Authorization;
using FinTrack.Application.Interfaces.Services;
using FinTrack.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(IAuthService authService, RefreshSessionService sessions, EmailVerificationService emailVerification, IWebHostEnvironment environment) : ControllerBase
{
    private const string RefreshCookieName = "fintrack_refresh";

    [HttpPost("register")]
    [EnableRateLimiting("auth-register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { message = result.Error });
        }

        return await CompleteOrChallengeAsync(result.Value, cancellationToken);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { message = result.Error });
        }

        return await CompleteOrChallengeAsync(result.Value, cancellationToken);
    }
    [HttpPost("verify-email")]
    [EnableRateLimiting("auth-email")]
    public async Task<IActionResult> VerifyEmail(ConfirmEmail request, CancellationToken ct)
    {
        var auth = await emailVerification.ConfirmAsync(request.ChallengeId, request.Code, ct);
        if (auth is null) return BadRequest(new { message = "Invalid or expired security code." });
        SetCookie(await sessions.StartAsync(auth.UserId, auth.PasswordVersion, ct));
        return Ok(auth);
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth-recovery-request")]
    public async Task<IActionResult> ForgotPassword(RecoveryEmail request,
        [FromServices] PasswordRecoveryService recovery, CancellationToken ct) =>
        Accepted(new { challengeId = await recovery.RequestAsync(request.Email.Trim(), ct) });

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth-recovery-reset")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request,
        [FromServices] PasswordRecoveryService recovery, CancellationToken ct)
    {
        if (request.Password != request.ConfirmPassword)
            return BadRequest(new { message = "Passwords must match." });
        if (!await recovery.ResetAsync(request.ChallengeId, request.Code, request.Password, ct))
            return BadRequest(new { message = "Invalid or expired security code." });

        Response.Cookies.Delete(RefreshCookieName, CookieSettings());
        return NoContent();
    }

    public sealed record RecoveryEmail([Required, EmailAddress, StringLength(150)] string Email);
    public sealed record ResetPasswordRequest(Guid ChallengeId,
        [Required, RegularExpression("^[0-9]{6}$")] string Code,
        [Required, StringLength(100, MinimumLength = 6)] string Password,
        [Required, StringLength(100, MinimumLength = 6)] string ConfirmPassword);

    private async Task<IActionResult> CompleteOrChallengeAsync(AuthResponse auth, CancellationToken ct)
    {
        if (await emailVerification.AcceptSessionAsync(auth.UserId, touch: true, ct))
        {
            SetCookie(await sessions.StartAsync(auth.UserId, auth.PasswordVersion, ct));
            return Ok(auth);
        }

        try
        {
            var challenge = await emailVerification.IssueAsync(auth, ct);
            return challenge is null
                ? StatusCode(429, new { message = "Wait a minute before requesting another code." })
                : Accepted(new { challengeId = challenge });
        }
        catch (Exception error) when (error is SmtpException or HttpRequestException ||
                                      (error is OperationCanceledException && !ct.IsCancellationRequested))
        {
            return StatusCode(503, new { message = "Could not send the security code. Try signing in again." });
        }
    }

    public sealed record ConfirmEmail(Guid ChallengeId, [Required, RegularExpression("^[0-9]{6}$")] string Code);

    [HttpPost("refresh")]
    [EnableRateLimiting("auth-session")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Headers.ContainsKey("X-FinTrack-Session")) return BadRequest();

        try
        {
            var result = await sessions.RefreshAsync(Request.Cookies[RefreshCookieName], cancellationToken);
            if (result is null)
            {
                return Unauthorized();
            }

            SetCookie(result.Value.Cookie);
            return Ok(result.Value.Auth);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("logout")]
    [EnableRateLimiting("auth-session")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!Request.Headers.ContainsKey("X-FinTrack-Session")) return BadRequest();

        await sessions.LogoutAsync(Request.Cookies[RefreshCookieName], cancellationToken);
        Response.Cookies.Delete(RefreshCookieName, CookieSettings());
        return NoContent();
    }

    [Authorize]
    [EnableRateLimiting("auth-privacy")]
    [HttpPost("privacy/export")]
    public async Task<IActionResult> Export(
        [FromBody] ConfirmPassword request, [FromServices] PrivacyService privacy,
        [FromServices] ICurrentUserService currentUser, CancellationToken ct)
    {
        var data = await privacy.ExportAsync(currentUser.UserId, request.Password, ct);
        return data is null ? StatusCode(StatusCodes.Status403Forbidden) : Ok(data);
    }

    [Authorize]
    [EnableRateLimiting("auth-privacy")]
    [HttpPost("privacy/delete")]
    public async Task<IActionResult> DeleteProfile(
        [FromBody] ConfirmPassword request, [FromServices] PrivacyService privacy,
        [FromServices] ICurrentUserService currentUser, CancellationToken ct)
    {
        if (!await privacy.DeleteAsync(currentUser.UserId, request.Password, ct))
            return StatusCode(StatusCodes.Status403Forbidden);

        Response.Cookies.Delete(RefreshCookieName, CookieSettings());
        return NoContent();
    }

    public sealed record ConfirmPassword([Required, StringLength(1024)] string Password);

    private void SetCookie(RefreshCookie cookie)
    {
        var options = CookieSettings();
        options.Expires = cookie.ExpiresAt;
        Response.Cookies.Append(RefreshCookieName, cookie.Value, options);
    }

    private CookieOptions CookieSettings() => new()
    {
        HttpOnly = true,
        Secure = Request.IsHttps || !environment.IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth"
    };
}
