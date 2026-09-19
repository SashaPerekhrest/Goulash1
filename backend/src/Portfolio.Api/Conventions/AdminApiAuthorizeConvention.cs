using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace Portfolio.Api.Conventions;

public sealed class AdminApiAuthorizeConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (!HasAdminRoute(controller))
        {
            return;
        }

        controller.Filters.Add(new AuthorizeFilter());
    }

    private static bool HasAdminRoute(ControllerModel controller)
    {
        return controller.Selectors.Any(HasAdminRoute)
            || controller.Actions.Any(action => action.Selectors.Any(HasAdminRoute));
    }

    private static bool HasAdminRoute(SelectorModel selector)
    {
        var template = selector.AttributeRouteModel?.Template;
        if (template is null)
        {
            return false;
        }

        var normalizedTemplate = template.TrimStart('/');
        return normalizedTemplate.Equals("api/admin", StringComparison.OrdinalIgnoreCase)
            || normalizedTemplate.StartsWith("api/admin/", StringComparison.OrdinalIgnoreCase);
    }
}
