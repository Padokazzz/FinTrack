using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinTrack.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException exception)
        {
            await HandleExceptionAsync(context, exception, HttpStatusCode.Unauthorized);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                HttpStatusCode.Conflict,
                "The data changed while this operation was being saved. Reload and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            ConstraintName: "FK_Transactions_Accounts_AccountId"
                or "FK_Transfers_Accounts_FromAccountId"
                or "FK_Transfers_Accounts_ToAccountId"
        } && HttpMethods.IsDelete(context.Request.Method)
          && context.Request.Path.StartsWithSegments("/api/accounts"))
        {
            await HandleExceptionAsync(
                context,
                exception,
                HttpStatusCode.Conflict,
                "This account has transactions or transfers. Remove the linked records before deleting the account.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            ConstraintName: "FK_RecurringRules_Accounts_AccountId" or "FK_RecurringRules_Categories_CategoryId"
        })
        {
            await HandleExceptionAsync(
                context,
                exception,
                HttpStatusCode.Conflict,
                "The account or category is linked to a recurring rule. Reload the data or remove the linked rule.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");

            await HandleExceptionAsync(
                context,
                exception,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        HttpStatusCode statusCode,
        string? customMessage = null)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            message = customMessage ?? exception.Message
        };

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}