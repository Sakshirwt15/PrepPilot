using Microsoft.AspNetCore.Mvc;
using PrepPilot.Application.Exceptions;

namespace PrepPilot.Api.Middleware;

/// <summary>Converts exceptions to RFC 7807 problem responses so controllers stay free of try/catch.</summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            BusinessRuleException => (StatusCodes.Status400BadRequest, "Request not allowed"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ExternalServiceException => (StatusCodes.Status502BadGateway, "AI service problem"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            // Never leak internal details for unexpected errors.
            Detail = status == StatusCodes.Status500InternalServerError ? null : ex.Message,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }
}
