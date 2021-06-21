using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RY.VideoDAProject.CustomMiddleware
{
    public class RyExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public RyExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                httpContext.Response.ContentType = "application/problem+json";
                var title = "An error occured: " + ex.Message;
                var details = ex.ToString();
                var problem = new ProblemDetails
                {
                    Status = 200,
                    Title = title,
                    Detail = details
                };
                var stream = httpContext.Response.Body;
                await JsonSerializer.SerializeAsync(stream, problem);
            }
        }
    }
}