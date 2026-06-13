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

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);

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
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
