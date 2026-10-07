using System.Net;
using System.Text.Json;

namespace NfgoOrderApi.Common;

/// <summary>Сущность не найдена → 404.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Конфликт данных (дубликат, объект используется) → 409.</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>Нарушено бизнес-правило → 400.</summary>
public class BusinessRuleException(string message) : Exception(message);

/// <summary>Переводит доменные исключения в понятные HTTP-ответы вида { "error": "..." }.</summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (ex is NotFoundException or ConflictException or BusinessRuleException)
        {
            var status = ex switch
            {
                NotFoundException => HttpStatusCode.NotFound,
                ConflictException => HttpStatusCode.Conflict,
                _ => HttpStatusCode.BadRequest
            };
            await WriteAsync(context, status, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Необработанная ошибка");
            await WriteAsync(context, HttpStatusCode.InternalServerError, "Внутренняя ошибка сервера");
        }
    }

    private static async Task WriteAsync(HttpContext context, HttpStatusCode status, string message)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
