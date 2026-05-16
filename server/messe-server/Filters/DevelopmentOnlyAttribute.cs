using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;

namespace Herrmann.MesseApp.Server.Filters;

/// <summary>
/// Action filter that returns 404 Not Found in non-Development environments.
/// Apply to controllers or actions that must never be reachable in Production.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DevelopmentOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (!env.IsDevelopment())
        {
            context.Result = new NotFoundResult();
        }
    }
}
