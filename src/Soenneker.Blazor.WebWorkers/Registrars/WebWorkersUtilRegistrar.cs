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
    public static IServiceCollection AddWebWorkersUtilAsScoped(this IServiceCollection services)
    {
        services.AddModuleImportUtilAsScoped();
        services.TryAddScoped<IWebWorkersInterop, WebWorkersInterop>();
        services.TryAddScoped<IWebWorkersUtil, WebWorkersUtil>();

        return services;
    }
}
