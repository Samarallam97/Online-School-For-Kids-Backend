using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace Infrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior that sets the current thread's culture
/// from the HTTP request before every command/query handler executes.
///
/// Lives in Infrastructure (not Application) because IHttpContextAccessor
/// and IRequestCultureFeature require ASP.NET Core types that are only
/// available to projects that reference the full framework (SDK.Web or
/// Microsoft.AspNetCore.Authentication.JwtBearer etc.).
/// Application uses Microsoft.NET.Sdk and must stay framework-agnostic.
/// </summary>
public class LocalizationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalizationBehavior(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is not null)
        {
            var feature = httpContext.Features.Get<IRequestCultureFeature>();
            if (feature is not null)
            {
                var culture = feature.RequestCulture.Culture;
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
        }

        return await next();
    }
}
