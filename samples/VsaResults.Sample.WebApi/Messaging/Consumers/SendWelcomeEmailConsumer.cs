using VsaResults.Messaging;
using VsaResults.Sample.WebApi.Messaging.Commands;

namespace VsaResults.Sample.WebApi.Messaging.Consumers;

/// <summary>
/// Consumes SendWelcomeEmail commands.
/// Demonstrates the command consumer pattern - handling orchestration commands.
/// Commands have exactly-once semantics: one consumer processes each command.
/// </summary>
public class SendWelcomeEmailConsumer(ILogger<SendWelcomeEmailConsumer> logger) : IConsumer<SendWelcomeEmail>
{
    public async Task<ErrorOr<Unit>> ConsumeAsync(
        ConsumeContext<SendWelcomeEmail> context,
        CancellationToken ct = default)
    {
        var command = context.Message;

        logger.LogInformation(
            "[SendWelcomeEmailConsumer] Sending welcome email to {Email} for user {UserId}",
            command.Email,
            command.UserId);

        // Simulate email sending delay
        await Task.Delay(100, ct);

        // In a real implementation, this would:
        // 1. Render email template with user's name
        // 2. Call email service (SendGrid, SES, Mailgun, etc.)
        // 3. Handle failures with ErrorOr

        // Example error handling:
        // if (await emailService.HasQuotaExceededAsync(ct))
        //     return MessagingErrors.ConsumerFailed(
        //         nameof(SendWelcomeEmailConsumer),
        //         "Email quota exceeded");
        logger.LogInformation(
            "[SendWelcomeEmailConsumer] Welcome email sent successfully to {Email}",
            command.Email);

        context.AddContext("email_sent_to", command.Email);
        context.AddContext("email_type", "welcome");

        return Unit.Value;
    }
}
