using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Herrmann.MesseApp.Server.Filters;

/// <summary>
/// Removes specific controller types from the MVC controller feature so their routes are
/// never registered. Apply at startup via <see cref="ApplicationPartManager.FeatureProviders"/>
/// to exclude development-only controllers from non-Development builds.
/// </summary>
internal sealed class ExcludeControllersFeatureProvider(params Type[] excluded)
    : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var type in excluded)
            feature.Controllers.Remove(type.GetTypeInfo());
    }
}
