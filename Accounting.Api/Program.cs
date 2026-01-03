using Accounting.Api.Middleware;
using Accounting.Application.Common.Behaviors;
using Accounting.Application.Services;
using Accounting.Infrastructure;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Persistence.Seed;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// Add services to the container.

// JSON'da parasal alanlar string ("1234.56") olarak gelebilir.
// Bu ayar, string gelen sayýlarý decimal'a baðlamamýza izin verir (precision kaybýný önler).
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.NumberHandling =
        System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Accounting.Api", Version = "v1" });
    c.MapType<decimal>(() => new OpenApiSchema { Type = "string", Format = "decimal" });
    c.MapType<decimal?>(() => new OpenApiSchema { Type = "string", Format = "decimal" });
});

builder.Services.AddProblemDetails();

// MediatR + FluentValidation
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Accounting.Application.Invoices.Commands.Create.CreateInvoiceCommand).Assembly));
builder.Services.AddValidatorsFromAssemblyContaining<Accounting.Application.Invoices.Commands.Create.CreateInvoiceValidator>();

// Pipeline Behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", p => p
        .WithOrigins(
            "http://localhost:3000", // Next.js / React
            "http://localhost:4200"  // Angular
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .DisallowCredentials()
    );
});

// Infrastructure (DbContext vs.)
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Middleware pipeline
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionToProblemDetailsMiddleware>();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        c.SwaggerEndpoint("v1/swagger.json", "Accounting.Api v1");
    });
}

app.UseHttpsRedirection();

// CORS
app.UseCors("Frontend");

app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health");

// Database Migration + Seeding
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var balanceService = scope.ServiceProvider.GetRequiredService<IInvoiceBalanceService>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db, balanceService);
}

app.Run();