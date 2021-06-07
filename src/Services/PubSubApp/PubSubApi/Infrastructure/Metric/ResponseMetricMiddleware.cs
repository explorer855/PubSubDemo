using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PubSubApi.Infrastructure.Metric
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ResponseMetricMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseMetricMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, PrometheusMetricReporter reporter)
        {

            var path = httpContext.Request.Path.Value;
            if (path == "/metrics")
            {
                await _next.Invoke(httpContext);
                return;
            }
            var sw = Stopwatch.StartNew();

            try
            {
                await _next.Invoke(httpContext);
            }
            finally
            {
                sw.Stop();
                reporter.RegisterRequest();
                reporter.RegisterResponseTime(httpContext.Response.StatusCode, httpContext.Request.Method, sw.Elapsed);
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ResponseMetricMiddlewareExtensions
    {
        public static IApplicationBuilder UseResponseMetricMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ResponseMetricMiddleware>();
        }
    }
}
