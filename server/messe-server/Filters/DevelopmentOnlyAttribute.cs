using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;

namespace Herrmann.MesseApp.Server.Filters;

/// <summary>
/// Action filter that returns 404 Not Found in non-Development environments.
/// Apply to controllers or actions that must never be reachable in Production.
/// </summary>
/// <remarks>
/// In non-Development environments, <see cref="ExcludeControllersFeatureProvider"/> removes
/// the decorated controller from the routing table entirely, so this filter never executes
/// in production. The filter serves as a defence-in-depth fallback: if the feature-provider
/// exclusion is ever bypassed (e.g. a future refactor removes the Program.cs guard), this
/// attribute ensures the endpoint still returns 404 rather than being reachable.
/// It also hides the endpoint from OpenAPI clients alongside
/// <see cref="Microsoft.AspNetCore.Mvc.ApiExplorer.ApiExplorerSettingsAttribute"/>.
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
