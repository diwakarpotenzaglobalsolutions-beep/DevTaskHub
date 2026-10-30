using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;

namespace DevTaskHubBackend.Middleware;

public class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Request {TraceId} was canceled by the client.", context.TraceIdentifier);
            return;
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(exception, "Unhandled exception after the response started. TraceId: {TraceId}", context.TraceIdentifier);
                throw;
            }

            var mapped = MapException(exception);
            if (mapped.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", context.TraceIdentifier);
            }
            else
            {
                _logger.LogWarning(exception, "Request failed with status {StatusCode}. TraceId: {TraceId}", mapped.StatusCode, context.TraceIdentifier);
            }

            await WriteAsync(context, mapped.StatusCode, mapped.Message, mapped.Errors, exception);
            return;
        }

        if (context.Response.StatusCode >= StatusCodes.Status400BadRequest
            && !context.Response.HasStarted
            && context.Response.ContentLength is null or 0
            && !HttpMethods.IsHead(context.Request.Method))
        {
            await WriteAsync(
                context,
                context.Response.StatusCode,
                HttpStatusMessages.Get(context.Response.StatusCode));
        }
    }

    public static IActionResult InvalidModelState(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The value is invalid."
                        : error.ErrorMessage)
                    .ToArray());

        return new BadRequestObjectResult(new ApiErrorResponse
        {
            StatusCode = StatusCodes.Status400BadRequest,
            Message = HttpStatusMessages.Get(StatusCodes.Status400BadRequest),
            TraceId = context.HttpContext.TraceIdentifier,
            Errors = errors
        });
    }

    private (int StatusCode, string Message, IReadOnlyDictionary<string, string[]>? Errors) MapException(Exception exception)
    {
        exception = Unwrap(exception);
        var mysql = FindMySqlException(exception);

        if (exception is AppException appException)
        {
            var message = string.IsNullOrWhiteSpace(appException.Message)
                ? HttpStatusMessages.Get(appException.StatusCode)
                : appException.Message;
            return (appException.StatusCode, message, null);
        }

        if (exception is BadHttpRequestException badRequest)
        {
            return (badRequest.StatusCode, HttpStatusMessages.Get(badRequest.StatusCode), null);
        }

        if (exception is ValidationException validation)
        {
            IReadOnlyDictionary<string, string[]>? errors = validation.ValidationResult.MemberNames.Any()
                ? validation.ValidationResult.MemberNames.ToDictionary(
                    name => name,
                    _ => new[] { validation.ValidationResult.ErrorMessage ?? "The value is invalid." })
                : null;
            return (StatusCodes.Status400BadRequest, validation.ValidationResult.ErrorMessage ?? HttpStatusMessages.Get(StatusCodes.Status400BadRequest), errors);
        }

        if (exception is UnauthorizedAccessException or SecurityTokenException)
        {
            return (StatusCodes.Status401Unauthorized, HttpStatusMessages.Get(StatusCodes.Status401Unauthorized), null);
        }

        if (exception is KeyNotFoundException)
        {
            return (StatusCodes.Status404NotFound, HttpStatusMessages.Get(StatusCodes.Status404NotFound), null);
        }

        if (exception is NotImplementedException)
        {
            return (StatusCodes.Status501NotImplemented, HttpStatusMessages.Get(StatusCodes.Status501NotImplemented), null);
        }

        if (exception is TimeoutException)
        {
            return (StatusCodes.Status504GatewayTimeout, HttpStatusMessages.Get(StatusCodes.Status504GatewayTimeout), null);
        }

        if (mysql is not null && exception is not DbUpdateException)
        {
            return MapMySql(mysql);
        }

        if (exception is DbUpdateConcurrencyException)
        {
            return (StatusCodes.Status409Conflict, HttpStatusMessages.Get(StatusCodes.Status409Conflict), null);
        }

        if (exception is DbUpdateException)
        {
            return mysql is null
                ? (StatusCodes.Status409Conflict, HttpStatusMessages.Get(StatusCodes.Status409Conflict), null)
                : MapMySql(mysql);
        }

        if (exception is ArgumentException or FormatException or JsonException)
        {
            return (StatusCodes.Status400BadRequest, exception.Message, null);
        }

        return (StatusCodes.Status500InternalServerError, HttpStatusMessages.Get(StatusCodes.Status500InternalServerError), null);
    }

    private static (int StatusCode, string Message, IReadOnlyDictionary<string, string[]>? Errors) MapMySql(MySqlException exception)
    {
        return exception.Number switch
        {
            1062 => (StatusCodes.Status409Conflict, "A record with the same value already exists.", null),
            1048 or 1406 or 3819 => (StatusCodes.Status400BadRequest, "The submitted data is not valid.", null),
            1452 => (StatusCodes.Status400BadRequest, "A related record does not exist.", null),
            1451 => (StatusCodes.Status409Conflict, "This record is still in use and cannot be changed.", null),
            1045 or 1049 or 2002 or 2003 or 2006 or 2013 => (StatusCodes.Status503ServiceUnavailable, HttpStatusMessages.Get(StatusCodes.Status503ServiceUnavailable), null),
            _ => (StatusCodes.Status500InternalServerError, HttpStatusMessages.Get(StatusCodes.Status500InternalServerError), null)
        };
    }

    private async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null,
        Exception? exception = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var includeDetail = exception is not null
            && statusCode >= StatusCodes.Status500InternalServerError
            && _environment.IsDevelopment();

        var body = new ApiErrorResponse
        {
            StatusCode = statusCode,
            Message = message,
            TraceId = context.TraceIdentifier,
            Errors = errors,
            Detail = includeDetail ? exception!.ToString() : null
        };

        await context.Response.WriteAsJsonAsync(body, JsonOptions);
    }

    private static Exception Unwrap(Exception exception)
    {
        if (exception is AggregateException aggregate)
        {
            return aggregate.Flatten().InnerExceptions.FirstOrDefault() ?? exception;
        }

        return exception;
    }

    private static MySqlException? FindMySqlException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is MySqlException mysql)
            {
                return mysql;
            }
        }

        return null;
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
