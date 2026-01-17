using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using VsaResults.WideEvents;

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
    public static IServiceCollection AddVsaFeatures(this IServiceCollection services, params Assembly[] assemblies)
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
    public static IServiceCollection AddVsaFeatures<T>(this IServiceCollection services) =>
        services.AddVsaFeatures(typeof(T).Assembly);

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

    /// <summary>
    /// Registers the unified wide events system with default options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUnifiedWideEvents(this IServiceCollection services)
    {
        return services.AddUnifiedWideEvents(_ => { });
    }

    /// <summary>
    /// Registers the unified wide events system with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUnifiedWideEvents(
        this IServiceCollection services,
        Action<WideEventOptions> configure)
    {
        var options = new WideEventOptions();
        configure(options);

        services.AddSingleton(options);

        // Register interceptors
        services.TryAddSingleton<SamplingInterceptor>();
        services.TryAddSingleton<RedactionInterceptor>();
        services.TryAddSingleton<ContextLimitInterceptor>();
        services.TryAddSingleton<VerbosityInterceptor>();

        // Register the interceptor collection
        services.TryAddSingleton<IEnumerable<IWideEventInterceptor>>(sp => new IWideEventInterceptor[]
        {
            sp.GetRequiredService<SamplingInterceptor>(),
            sp.GetRequiredService<RedactionInterceptor>(),
            sp.GetRequiredService<ContextLimitInterceptor>(),
            sp.GetRequiredService<VerbosityInterceptor>(),
        });

        // Register in-memory sink as default (users should override with their own sink)
        services.TryAddSingleton<IWideEventSink, InMemoryWideEventSink>();

        // Register the unified emitter
        services.TryAddSingleton<IUnifiedWideEventEmitter>(sp =>
        {
            var sink = sp.GetRequiredService<IWideEventSink>();
            var interceptors = sp.GetRequiredService<IEnumerable<IWideEventInterceptor>>();
            return new UnifiedWideEventEmitter(sink, interceptors);
        });

        // Register legacy adapter for backward compatibility
        services.TryAddSingleton<IWideEventEmitter>(sp =>
            new UnifiedToLegacyEmitterAdapter(sp.GetRequiredService<IUnifiedWideEventEmitter>()));

        return services;
    }

    /// <summary>
    /// Registers a custom sink for the unified wide events system.
    /// </summary>
    /// <typeparam name="TSink">The sink implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWideEventSink<TSink>(this IServiceCollection services)
        where TSink : class, IWideEventSink
    {
        services.AddSingleton<IWideEventSink, TSink>();
        return services;
    }

    /// <summary>
    /// Registers a custom sink instance for the unified wide events system.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="sink">The sink instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWideEventSink(this IServiceCollection services, IWideEventSink sink)
    {
        services.AddSingleton(sink);
        return services;
    }

    /// <summary>
    /// Registers a custom interceptor for the unified wide events system.
    /// </summary>
    /// <typeparam name="TInterceptor">The interceptor implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWideEventInterceptor<TInterceptor>(this IServiceCollection services)
        where TInterceptor : class, IWideEventInterceptor
    {
        services.AddSingleton<IWideEventInterceptor, TInterceptor>();
        return services;
    }

    /// <summary>
    /// Registers a Serilog-compatible sink using ILogger.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSerilogWideEventSink(this IServiceCollection services)
    {
        services.AddSingleton<IWideEventSink>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            return new SerilogWideEventSink(loggerFactory);
        });
        return services;
    }

    /// <summary>
    /// Registers a structured logging sink using ILogger (includes all event properties).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStructuredLogWideEventSink(this IServiceCollection services)
    {
        services.AddSingleton<IWideEventSink>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            return new StructuredLogWideEventSink(loggerFactory.CreateLogger("WideEvent"));
        });
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
