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
            // Health/readiness endpoints are deliberately excluded from
            // versioning - infrastructure tooling (load balancers, platform
            // health checks) conventionally assumes a stable, unversioned
            // path here. A production incident (Northflank's health check
            // 404ing against the new /v1 prefix, taking the service out of
            // rotation) confirmed this the hard way.
            if (controller.ControllerName == "Health") continue;

            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel = selector.AttributeRouteModel != null
                    ? AttributeRouteModel.CombineAttributeRouteModel(_prefix, selector.AttributeRouteModel)
                    : _prefix;
            }
        }
    }
}
