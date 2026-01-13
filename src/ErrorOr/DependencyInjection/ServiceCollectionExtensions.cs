using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

namespace VsaResults;

/// <summary>
/// Extension methods for registering ErrorOr feature services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all ErrorOr feature-related services from the specified assemblies.
    /// Discovers and registers implementations of feature interfaces and their stages.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for feature implementations.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddErrorOrFeatures(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            RegisterFeaturesFromAssembly(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// Registers all ErrorOr feature-related services from the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T">A type in the assembly to scan.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddErrorOrFeatures<T>(this IServiceCollection services) =>
        services.AddErrorOrFeatures(typeof(T).Assembly);

    /// <summary>
    /// Registers a specific wide event emitter.
    /// </summary>
    /// <typeparam name="TEmitter">The emitter implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWideEventEmitter<TEmitter>(this IServiceCollection services)
        where TEmitter : class, IWideEventEmitter
    {
        services.AddSingleton<IWideEventEmitter, TEmitter>();
        return services;
    }

    /// <summary>
    /// Registers a wide event emitter instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="emitter">The emitter instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWideEventEmitter(this IServiceCollection services, IWideEventEmitter emitter)
    {
        services.AddSingleton(emitter);
        return services;
    }

    private static void RegisterFeaturesFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false });

        foreach (var type in types)
        {
            RegisterIfImplementsFeatureInterface(services, type);
            RegisterIfImplementsStageInterface(services, type);
        }
    }

    private static void RegisterIfImplementsFeatureInterface(IServiceCollection services, Type type)
    {
        var interfaces = type.GetInterfaces();

        foreach (var @interface in interfaces)
        {
            if (!@interface.IsGenericType)
            {
                continue;
            }

            var genericDef = @interface.GetGenericTypeDefinition();

            // Register mutation features
            if (genericDef == typeof(IMutationFeature<,>))
            {
                // Register the concrete implementation for the interface
                services.AddScoped(@interface, type);

                // Also register for any parent interface if exists
                foreach (var parentInterface in type.GetInterfaces().Where(i => i != @interface && i.IsAssignableTo(@interface)))
                {
                    services.AddScoped(parentInterface, sp => sp.GetRequiredService(@interface));
                }
            }

            // Register query features
            if (genericDef == typeof(IQueryFeature<,>))
            {
                services.AddScoped(@interface, type);

                foreach (var parentInterface in type.GetInterfaces().Where(i => i != @interface && i.IsAssignableTo(@interface)))
                {
                    services.AddScoped(parentInterface, sp => sp.GetRequiredService(@interface));
                }
            }
        }
    }

    private static void RegisterIfImplementsStageInterface(IServiceCollection services, Type type)
    {
        var interfaces = type.GetInterfaces();

        foreach (var @interface in interfaces)
        {
            if (!@interface.IsGenericType)
            {
                continue;
            }

            var genericDef = @interface.GetGenericTypeDefinition();

            // Register validators
            if (genericDef == typeof(IFeatureValidator<>))
            {
                services.AddScoped(@interface, type);
            }

            // Register requirements
            if (genericDef == typeof(IFeatureRequirements<>))
            {
                services.AddScoped(@interface, type);
            }

            // Register mutators
            if (genericDef == typeof(IFeatureMutator<,>))
            {
                services.AddScoped(@interface, type);
            }

            // Register queries
            if (genericDef == typeof(IFeatureQuery<,>))
            {
                services.AddScoped(@interface, type);
            }

            // Register side effects
            if (genericDef == typeof(IFeatureSideEffects<>))
            {
                services.AddScoped(@interface, type);
            }
        }
    }
}
