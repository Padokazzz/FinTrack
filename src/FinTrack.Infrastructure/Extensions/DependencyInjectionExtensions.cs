using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Infrastructure.Repositories;

namespace FinTrack.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    services.AddDbContext<FinTrackDbContext>(options =>
        options.UseNpgsql(connectionString));

    services.AddScoped<IUnitOfWork, UnitOfWork>();

    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IAccountRepository, AccountRepository>();
    services.AddScoped<ICategoryRepository, CategoryRepository>();
    services.AddScoped<ITransactionRepository, TransactionRepository>();

    return services;
}
}