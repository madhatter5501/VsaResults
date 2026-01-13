namespace VsaResults;

/// <summary>
/// Optional interface for requests that need complex context extraction logic.
/// Implement this when simple property attributes aren't sufficient.
/// </summary>
/// <example>
/// <code>
/// public sealed record Request(Guid UserId, List&lt;string&gt; Tags) : IWideEventContext
/// {
///     public IEnumerable&lt;KeyValuePair&lt;string, object?&gt;&gt; GetWideEventContext()
///     {
///         yield return new("user_id", UserId);
///         yield return new("tag_count", Tags.Count);
///         yield return new("first_tag", Tags.FirstOrDefault());
///     }
/// }
/// </code>
/// </example>
public interface IWideEventContext
{
    /// <summary>
    /// Returns context to be included in the wide event.
    /// </summary>
    /// <returns>Key-value pairs to include in the wide event context.</returns>
    IEnumerable<KeyValuePair<string, object?>> GetWideEventContext();
}
