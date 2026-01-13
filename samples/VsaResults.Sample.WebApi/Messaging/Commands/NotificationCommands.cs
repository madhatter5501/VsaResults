using VsaResults.Messaging;

namespace VsaResults.Sample.WebApi.Messaging.Commands;

/// <summary>
/// Command to send a welcome email to a newly created user.
/// This demonstrates the orchestration pattern - sending a command
/// as part of the mutation to coordinate with an external service.
/// </summary>
public record SendWelcomeEmail(
    Guid UserId,
    string Email,
    string Name) : ICommand;

/// <summary>
/// Command to send a goodbye/account deletion confirmation email.
/// </summary>
public record SendGoodbyeEmail(
    Guid UserId,
    string Email) : ICommand;
