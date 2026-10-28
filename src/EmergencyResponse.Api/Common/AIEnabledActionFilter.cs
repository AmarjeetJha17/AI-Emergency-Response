using EmergencyResponse.Application.Common.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Api.Common;

public sealed class AIEnabledActionFilter : IAsyncActionFilter
{
    private readonly AIOptions _options;

    public AIEnabledActionFilter(IOptions<AIOptions> options)
    {
        _options = options.Value;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (_options.Enabled)
        {
            await next();
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "AI feature is disabled.",
            Detail = "This operation requires AI, and AI is disabled for this deployment.",
            Instance = context.HttpContext.Request.Path
        })
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable
        };

    }
}
