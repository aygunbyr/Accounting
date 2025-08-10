using Accounting.Application.Common.Behaviors;
using Accounting.Infrastructure; // Ensure this namespace is included for extension methods
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Persistence.Seed;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Accounting.Api", Version = "v1" });
});

// ProblemDetails
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        // traceId gibi faydalý bir alan ekleyelim
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});

// MediatR (Application assembly taramasý)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Accounting.Application.Invoices.Commands.Create.CreateInvoiceCommand).Assembly));

// FluentValidation (DI taramasý)
builder.Services.AddValidatorsFromAssemblyContaining<Accounting.Application.Invoices.Commands.Create.CreateInvoiceValidator>();

// Pipeline Behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Infrastructure (DbContext vs.)
builder.Services.AddInfrastructure(builder.Configuration); // Ensure the AddInfrastructure extension method is implemented and accessible

var app = builder.Build();

// Middleware pipeline
app.UseSerilogRequestLogging(); // basit request log

// Exceptionlarý ProblemDetails olarak dönmesi için
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";          // UI: /swagger
        c.SwaggerEndpoint("v1/swagger.json", "Accounting.Api v1"); // JSON: /swagger/v1/swagger.json
    });
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db);
}

app.Run();
