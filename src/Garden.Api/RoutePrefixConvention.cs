using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Garden.Api;

/// <summary>
/// RFC-018 (specification/RFC/RFC-018-ai-narrator-api-hardening.md): adds
/// TG-DEV-009's "API versioning" via a single global route prefix, rather
/// than editing all 16 controllers individually or pulling in a full
/// versioning framework - there is only one API version that has ever
/// existed, so a route-prefix convention is the smallest correct fix.
/// </summary>
public class RoutePrefixConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel _prefix;

    public RoutePrefixConvention(string prefix)
    {
        _prefix = new AttributeRouteModel(new Microsoft.AspNetCore.Mvc.RouteAttribute(prefix));
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel = selector.AttributeRouteModel != null
                    ? AttributeRouteModel.CombineAttributeRouteModel(_prefix, selector.AttributeRouteModel)
                    : _prefix;
            }
        }
    }
}
