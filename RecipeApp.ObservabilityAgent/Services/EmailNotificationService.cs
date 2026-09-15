using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;

namespace RecipeApp.ObservabilityAgent.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly IResend _resend;
    private readonly string _from;
    private readonly string _to;
    private readonly string _name;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(IResend resend, IConfiguration configuration, ILogger<EmailNotificationService> logger)
    {
        _resend = resend;
        _from = configuration["Resend:FromEmail"]
            ?? throw new InvalidOperationException("Resend:FromEmail não configurado.");
        _to = configuration["Resend:ToEmail"]
            ?? throw new InvalidOperationException("Resend:ToEmail não configurado.");
        _name = configuration["Resend:FromName"] ?? throw new InvalidOperationException("Resend:FromName não configurado.");
        _logger = logger;
    }

    public async Task SendAsync(string subject, string body)
    {
        var message = new EmailMessage
        {
            From = $"{_name} <{_from}>",
            Subject = subject,
            HtmlBody = $"<pre style=\"font-family: monospace; white-space: pre-wrap;\">{body}</pre>"
        };
        message.To.Add(_to);

        await _resend.EmailSendAsync(message);

        _logger.LogInformation("E-mail de análise enviado via Resend.");
    }
}