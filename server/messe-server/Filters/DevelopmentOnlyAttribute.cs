using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;

namespace Herrmann.MesseApp.Server.Filters;

/// <summary>
/// Action filter that returns 404 Not Found in non-Development environments.
/// Apply to controllers or actions that must never be reachable in Production.
/// </summary>
/// <remarks>
/// The controller and its routes remain registered in the routing table. This is
/// acceptable for a single-user local application where environment misconfiguration
/// is not a realistic threat vector. The filter provides a defence-in-depth check
/// alongside <see cref="Microsoft.AspNetCore.Mvc.ApiExplorer.ApiExplorerSettingsAttribute"/>
/// which hides the endpoint from generated OpenAPI clients.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DevelopmentOnlyAttribute : ActionFilterAttribute
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
