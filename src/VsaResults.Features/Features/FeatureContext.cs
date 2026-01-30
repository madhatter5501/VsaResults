namespace VsaResults;

/// <summary>
/// Carries the request through the feature pipeline along with loaded entities and wide event context.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <remarks>
/// <para>
/// <strong>Context System Overview:</strong> VsaResults has two context mechanisms:
/// </para>
/// <list type="bullet">
///   <item>
///     <term><c>FeatureContext&lt;TRequest&gt;.AddContext()</c> (this class)</term>
///     <description>
///       Mutable context scoped to a Feature Pipeline execution.
///       Use within pipeline stages (<c>IFeatureValidator</c>, <c>IFeatureRequirements</c>,
///       <c>IFeatureMutator</c>, <c>IFeatureSideEffects</c>).
///       Automatically merged into the wide event at the end of execution.
///     </description>
///   </item>
///   <item>
///     <term><c>ErrorOr&lt;T&gt;.WithContext()</c></term>
///     <description>
///       Immutable context that flows through fluent chains (<c>Then</c>, <c>Else</c>, <c>Match</c>, etc.).
///       Use for ad-hoc operations or when not using the Feature Pipeline.
///     </description>
///   </item>
/// </list>
/// <para>
/// <strong>Usage:</strong> When implementing feature pipeline stages, use <c>context.AddContext()</c>
/// to add business-relevant data that should appear in the wide event log.
/// </para>
/// </remarks>
public sealed class FeatureContext<TRequest>
{
    private readonly Dictionary<string, object> _entities = new();
    private readonly Dictionary<string, object?> _wideEventContext = new();

    /// <summary>
    /// Gets the validated request being processed.
    /// </summary>
    public required TRequest Request { get; init; }

    /// <summary>
    /// Gets the entities loaded during requirements enforcement.
    /// Use SetEntity/GetEntity for type-safe access.
    /// </summary>
    public IReadOnlyDictionary<string, object> Entities => _entities;

    /// <summary>
    /// Gets the context to be included in the wide event log.
    /// Add business-relevant data here during feature execution.
    /// </summary>
    public IReadOnlyDictionary<string, object?> WideEventContext => _wideEventContext;

    /// <summary>
    /// Gets a previously stored entity by key.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="key">The storage key.</param>
    /// <returns>The entity cast to the specified type.</returns>
    public T GetEntity<T>(string key) => (T)_entities[key];

    /// <summary>
    /// Tries to get a previously stored entity by key.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="key">The storage key.</param>
    /// <param name="value">The entity if found.</param>
    /// <returns>True if the entity was found.</returns>
    public bool TryGetEntity<T>(string key, out T? value)
    {
        if (_entities.TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Stores an entity for later retrieval in the pipeline.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="key">The storage key.</param>
    /// <param name="value">The entity to store.</param>
    public void SetEntity<T>(string key, T value)
        where T : notnull
        => _entities[key] = value;

    /// <summary>
    /// Adds context to be included in the wide event log.
    /// Use snake_case keys for consistency with structured logging.
    /// </summary>
    /// <param name="key">The context key (e.g., "user_id", "order_count").</param>
    /// <param name="value">The context value.</param>
    /// <returns>This context for fluent chaining.</returns>
    public FeatureContext<TRequest> AddContext(string key, object? value)
    {
        _wideEventContext[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple context entries to be included in the wide event log.
    /// </summary>
    /// <param name="pairs">Key-value pairs to add.</param>
    /// <returns>This context for fluent chaining.</returns>
    public FeatureContext<TRequest> AddContext(params (string Key, object? Value)[] pairs)
    {
        foreach (var (key, value) in pairs)
        {
            _wideEventContext[key] = value;
        }

        return this;
    }
}
