using System.Net;
using System.Text.Json;

namespace Hedger.Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
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

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // log everything
            _logger.LogError(ex, "Unhandled exception occurred while processing the request.");

            context.Response.ContentType = "application/json";

            // default status code
            var statusCode = HttpStatusCode.InternalServerError;
            string errorCode = "internal_error";

            // you can map specific exception types here if you want
            if (ex is ArgumentException)
            {
                statusCode = HttpStatusCode.BadRequest;
                errorCode = "bad_request";
            }
            else if (ex is FileNotFoundException)
            {
                statusCode = HttpStatusCode.NotFound;
                errorCode = "file_not_found";
            }

            context.Response.StatusCode = (int)statusCode;

            var problem = new
            {
                type = $"https://httpstatuses.com/{(int)statusCode}",
                title = "An error occurred while processing your request.",
                status = (int)statusCode,
                code = errorCode,
                detail = ex.Message,
                traceId = context.TraceIdentifier
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(problem, options);
            await context.Response.WriteAsync(json);
        }
    }
}
