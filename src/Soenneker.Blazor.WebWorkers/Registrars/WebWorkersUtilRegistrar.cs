using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Blazor.WebWorkers.Abstract;
using Soenneker.Blazor.Utils.ModuleImport.Registrars;

namespace Soenneker.Blazor.WebWorkers.Registrars;

/// <summary>
/// Registration for the interop and utility services.
/// </summary>
public static class WebWorkersUtilRegistrar
{
    /// <summary>
    /// Adds the shared web worker utility and underlying interop as scoped services.
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddWebWorkersUtilAsScoped(this IServiceCollection services)
    {
        services.AddModuleImportUtilAsScoped();
        services.TryAddScoped<IWebWorkersInterop, WebWorkersInterop>();
        services.TryAddScoped<IWebWorkersUtil, WebWorkersUtil>();

        return services;
    }
}
