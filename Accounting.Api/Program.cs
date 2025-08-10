using Serilog;
using Accounting.Infrastructure; // Ensure this namespace is included for extension methods

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ProblemDetails
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        // traceId gibi faydalý bir alan ekleyelim
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});

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
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
