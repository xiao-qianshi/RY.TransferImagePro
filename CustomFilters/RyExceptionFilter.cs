using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace RY.VideoDAProject.CustomFilters
{
    public class RyExceptionFilter : IAsyncExceptionFilter
    {
        private readonly ILogger<RyExceptionFilter> _logger;

        public RyExceptionFilter(ILogger<RyExceptionFilter> logger)
        {
            _logger = logger;
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            //throw new NotImplementedException();
            if (context.ExceptionHandled == false)
            {
                var msg = context.Exception.Message;
                context.Result = new ContentResult
                {
                    Content = msg,
                    ContentType = "application/problem+json",
                    StatusCode = StatusCodes.Status200OK
                };
                _logger.LogError(context.Exception, msg);
            }

            context.ExceptionHandled = true;
            return Task.CompletedTask;
        }
    }
}