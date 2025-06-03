using Azure;
using Azure.Communication.Email;
using Azure.Messaging.ServiceBus;
using EmailFunction.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmailFunction.Functions;

public class EmailSenderFunction(ILogger<EmailSenderFunction> logger, EmailClient emailClient)
{
    private readonly ILogger<EmailSenderFunction> _logger = logger;
    private readonly EmailClient _emailClient = emailClient;

    [Function(nameof(EmailSenderFunction))]
    public async Task Run([ServiceBusTrigger("email_request", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        try
        {
            var messageBody = message.Body.ToString();
            var emailRequest = System.Text.Json.JsonSerializer.Deserialize<EmailRequestModel>(messageBody);
            if (emailRequest != null && SendEmail(emailRequest))
                await messageActions.CompleteMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : EmailSenderFunction.Run :: {ex.Message}");
        }
        await messageActions.DeadLetterMessageAsync(message);
    }

    private bool SendEmail(EmailRequestModel emailRequest)
    {
        var result = _emailClient.Send(WaitUntil.Completed,
            senderAddress: Environment.GetEnvironmentVariable("SenderAddress"),
            recipientAddress: emailRequest.To,
            subject: emailRequest.Subject,
            htmlContent: emailRequest.HtmlContent,
            plainTextContent: emailRequest.PlainTextContent
        );
        return result.HasCompleted;
    }
}