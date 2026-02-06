using System.Net;
using System.Text.Json;
using FluentValidation;
using Domain.Exceptions;

namespace API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            Message = exception.Message,
            Errors = new List<string>()
        };

        switch (exception)
        {
            case ValidationException validationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Validation failed";
                errorResponse.Errors = validationException.Errors
                    .Select(e => e.ErrorMessage)
                    .ToList();
                break;

            case UserNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            case InvalidTokenException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Message = "Invalid or expired token";
                break;

            case UnauthorizedException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case DomainException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = "An internal server error occurred";

                // Hide implementation details in production
                if (!context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                {
                    errorResponse.Errors = new List<string>();
                }
                else
                {
                    errorResponse.Errors = new List<string> { exception.StackTrace ?? string.Empty };
                }
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}