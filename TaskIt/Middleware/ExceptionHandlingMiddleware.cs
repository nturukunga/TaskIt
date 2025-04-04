using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TaskIt.Models;

namespace TaskIt.Middleware
{
    /// <summary>
    /// Middleware for global exception handling
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(exception, "An unhandled exception occurred.");

            context.Response.ContentType = "application/json";
            
            var (statusCode, message) = GetStatusCodeAndMessage(exception);
            context.Response.StatusCode = statusCode;

            // Handle API requests differently from MVC requests
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var result = JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        message = message,
                        detail = exception.Message,
                        stackTrace = context.Request.Headers["X-Environment"] == "Development" 
                            ? exception.StackTrace 
                            : null
                    }
                });

                await context.Response.WriteAsync(result);
            }
            else
            {
                // For MVC requests, redirect to the error page
                var errorViewModel = new ErrorViewModel
                {
                    RequestId = context.TraceIdentifier,
                    ErrorMessage = message,
                    ErrorCode = statusCode.ToString()
                };

                context.Items["ErrorViewModel"] = errorViewModel;
                context.Request.Path = "/Home/Error";
                
                await _next(context);
            }
        }

        private (int statusCode, string message) GetStatusCodeAndMessage(Exception exception)
        {
            // You can add more exception types as needed
            return exception switch
            {
                ArgumentNullException => (400, "One or more required arguments were missing or null."),
                ArgumentException => (400, "Invalid argument provided."),
                UnauthorizedAccessException => (401, "Unauthorized access."),
                KeyNotFoundException => (404, "The requested resource was not found."),
                InvalidOperationException => (400, "Invalid operation."),
                TimeoutException => (408, "The operation timed out."),
                _ => (500, "An unexpected error occurred. Please try again later.")
            };
        }
    }
}
