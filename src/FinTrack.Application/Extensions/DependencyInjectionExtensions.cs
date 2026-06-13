using FinTrack.Application.Interfaces.Services;
using FinTrack.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Application.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ISummaryService, SummaryService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}