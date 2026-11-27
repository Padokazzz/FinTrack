using System.Net;
using System.Security.Claims;
using FinTrack.Infrastructure.Privacy;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using FinTrack.Api.Extensions;
using FinTrack.Infrastructure.Extensions;
using FinTrack.Application.Extensions;
using FinTrack.Api.Services;
using FinTrack.Application.Interfaces.Services;
using FluentValidation;
using FinTrack.Application.DTOs.Auth;
using FinTrack.Api.Models;
using Microsoft.AspNetCore.Mvc;
using FinTrack.Api.Middlewares;
using FinTrack.Api.Filters;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using FinTrack.Infrastructure.Recurrences;
using FinTrack.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCorsPolicy";

if (builder.Environment.IsProduction())
{
    var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
    if (jwt is null || string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience) ||
        string.IsNullOrWhiteSpace(jwt.Secret) || Encoding.UTF8.GetByteCount(jwt.Secret) < 32 ||
        jwt.Secret.StartsWith("CHANGE_THIS", StringComparison.OrdinalIgnoreCase) ||
        jwt.ExpirationMinutes is < 1 or > 15)
        throw new InvalidOperationException("Configure a strong JWT secret, issuer, audience and a lifetime of 1 to 15 minutes.");

    var hosts = builder.Configuration["AllowedHosts"] ?? "";
    if (string.IsNullOrWhiteSpace(hosts) || hosts.Split(';', StringSplitOptions.TrimEntries).Contains("*"))
        throw new InvalidOperationException("Configure the public AllowedHosts in production.");

    if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        throw new InvalidOperationException("Run database migrations separately in production.");
}

var emailVerification = builder.Configuration.GetSection("EmailVerification").Get<EmailVerificationSettings>() ?? new();
if (emailVerification.InactiveDays < 1 || emailVerification.VerifyEveryDays is < 1 or > 365 ||
    emailVerification.InactiveDays > emailVerification.VerifyEveryDays ||
    (!builder.Environment.IsDevelopment() && (!emailVerification.Enabled ||
        (string.IsNullOrWhiteSpace(emailVerification.Host) && string.IsNullOrWhiteSpace(emailVerification.BrevoApiKey)) ||
        string.IsNullOrWhiteSpace(emailVerification.From) ||
        !string.IsNullOrEmpty(emailVerification.PickupDirectory))))
    throw new InvalidOperationException("Configure email verification and a secure SMTP relay.");

builder.Services.Configure<EmailVerificationSettings>(builder.Configuration.GetSection("EmailVerification"));
builder.Services.AddScoped<PasswordRecoveryService>();
builder.Services.AddHttpClient<EmailVerificationService>(client => client.Timeout = TimeSpan.FromSeconds(15))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

var trustedProxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
if (trustedProxies.Length > 0)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (var proxy in trustedProxies)
            options.KnownProxies.Add(IPAddress.Parse(proxy));
    });
}

var limitAuth = builder.Environment.IsProduction() ||
    builder.Configuration.GetValue<bool>("Security:LimitAuthRequests");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth-privacy", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    foreach (var (name, permits) in new[] { ("auth-login", 10), ("auth-register", 5), ("auth-session", 30), ("auth-email", 15), ("auth-recovery-request", 5), ("auth-recovery-reset", 15) })
    {
        options.AddPolicy(name, context => limitAuth
            ? RateLimitPartition.GetSlidingWindowLimiter(
                (name, context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown"),
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = permits,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueLimit = 0
                })
            : RateLimitPartition.GetNoLimiter((name, "development")));
    }
});

builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<FinTrack.Infrastructure.Insights.SpendingInsights>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById(
    builder.Configuration["Recurrences:TimeZone"] ?? "America/Sao_Paulo"));
builder.Services.AddScoped<RecurrenceService>();
builder.Services.AddScoped<IRecurrenceService>(services => services.GetRequiredService<RecurrenceService>());
builder.Services.AddHostedService<RecurrenceWorker>();
builder.Services.AddScoped<RefreshSessionService>();
builder.Services.AddScoped<PrivacyService>();
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error => new ValidationError
            {
                Field = entry.Key,
                Message = error.ErrorMessage
            }))
            .ToList();

        var response = new ValidationErrorResponse
        {
            Errors = errors
        };

        return new BadRequestObjectResult(response);
    };
});

builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequest>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FinTrack API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            new List<string>()
        }
    });
});

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
    dbContext.Database.Migrate();
}

if (trustedProxies.Length > 0)
    app.UseForwardedHeaders();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// The ingress must enforce HTTPS when application redirection is disabled.
if (!builder.Configuration.GetValue<bool>("ReverseProxy:EnforcesHttps"))
    app.UseHttpsRedirection();

var serveWeb = builder.Configuration.GetValue<bool>("Web:ServeStatic");
if (serveWeb)
{
    var index = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html");
    if (!File.Exists(index))
        throw new InvalidOperationException("The FinTrack web build is missing.");

    app.Use(async (context, next) =>
    {
        context.Response.Headers.ContentSecurityPolicy =
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        await next();
    });

    var contentTypes = new FileExtensionContentTypeProvider();
    contentTypes.Mappings[".webmanifest"] = "application/manifest+json";
    app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), web =>
        web.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = contentTypes,
            OnPrepareResponse = context =>
            {
                if (context.File.Name == "index.html")
                    context.Context.Response.Headers.CacheControl = "no-cache";
            }
        }));

    app.MapFallback(async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-cache";
        await context.Response.SendFileAsync(index, context.RequestAborted);
    }).WithMetadata(new HttpMethodMetadata([HttpMethods.Get, HttpMethods.Head]));
}

app.UseRouting();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
