using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Middleware;

public sealed class ExceptionToProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionToProblemDetailsMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionToProblemDetailsMiddleware(
        RequestDelegate next,
        ILogger<ExceptionToProblemDetailsMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException vex)
        {
            // FluentValidation -> 400 + ProblemDetails
            var errors = vex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var pd = new ProblemDetails
            {
                Title = "Validation error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Request validation failed."
            };
            pd.Extensions["errors"] = errors;
            pd.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                pd,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );
        }
        catch (KeyNotFoundException knf)
        {
            var pd = new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = knf.Message
            };
            pd.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/problem+json";

            await JsonSerializer.SerializeAsync(
                context.Response.Body, pd,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );
        }
        catch (Exception ex)
        {
            // Diğer her şey -> 500 + ProblemDetails
            _logger.LogError(ex, "Unhandled exception");

            var pd = new ProblemDetails
            {
                Title = "Unexpected error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = _env.IsDevelopment() ? ex.ToString() : "An error occurred."
            };
            pd.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                pd,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );
        }
    }
}
