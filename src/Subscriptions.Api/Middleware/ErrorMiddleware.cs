using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Subscriptions.Api.Contracts;
using Subscriptions.Business.Errors;
namespace Subscriptions.Api.Middleware;


public sealed class ErrorMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILogger<ErrorMiddleware> logger)
    {
        try
        {
            await next(context);
        }
        catch (BusinessException ex)
        {
            context.Response.StatusCode = ex.Status;
            if (ex.Status == 401) context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsJsonAsync(new ErrorResponse(ex.Code, ex.Message, context.TraceIdentifier, ex.Errors));
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            var database = ex is SqliteException or DbUpdateException;
            context.Response.StatusCode = database ? 503 : 500;
            // Never log request bodies, tokens, database details or exception messages.
            logger.LogError("Request {RequestId} failed with {ExceptionType}", context.TraceIdentifier, ex.GetType().Name);
            await context.Response.WriteAsJsonAsync(new ErrorResponse(database ? "database_unavailable" : "internal_error",
                database ? "База данных временно недоступна." : "Внутренняя ошибка.", context.TraceIdentifier));
        }
    }
}
