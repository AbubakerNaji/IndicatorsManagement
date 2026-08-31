using System.Net;
using System.Text.Json;
using FluentValidation;
using IndicatorsManagement.Contracts.Responses;

namespace IndicatorsManagement.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        var correlationId = context.Response.Headers.TryGetValue("X-Correlation-Id", out var cid)
            ? cid.ToString() : context.TraceIdentifier;

        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                ApiResponse.Fail("خطأ في التحقق من البيانات", validationEx.Errors.Select(e => e.ErrorMessage).ToList())
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                ApiResponse.Fail("غير مصرح بالوصول")
            ),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                ApiResponse.Fail("العنصر المطلوب غير موجود")
            ),
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                ApiResponse.Fail(argEx.Message)
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                ApiResponse.Fail("حدث خطأ داخلي في الخادم. يرجى المحاولة لاحقاً.")
            )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception. CorrelationId={CorrelationId}, Path={Path}",
                correlationId, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Handled exception: {ExceptionType} at {Path}. CorrelationId={CorrelationId}",
                exception.GetType().Name, context.Request.Path, correlationId);
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
