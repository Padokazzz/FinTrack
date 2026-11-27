using FinTrack.Application.DTOs.Auth;
using FinTrack.Application.Interfaces.Services;
using FinTrack.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(IAuthService authService, RefreshSessionService sessions, IWebHostEnvironment environment) : ControllerBase
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

        SetCookie(await sessions.StartAsync(result.Value.UserId, cancellationToken));
        return Ok(result.Value);
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

        SetCookie(await sessions.StartAsync(result.Value.UserId, cancellationToken));
        return Ok(result.Value);
    }
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
