using VsaResults.Messaging;
using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Messaging.Events;

/// <summary>
/// Published after a new user is successfully created.
/// Subscribers can react to sync search indexes, send to analytics, etc.
/// </summary>
public record UserCreated(
    Guid UserId,
    string Email,
    string Name,
    UserRole Role,
    DateTime CreatedAt) : IEvent;

/// <summary>
/// Published after a user is successfully deleted.
/// Subscribers can clean up related data, update caches, etc.
/// </summary>
public record UserDeleted(
    Guid UserId,
    string Email,
    DateTime DeletedAt) : IEvent;
