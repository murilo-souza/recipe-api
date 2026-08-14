using Microsoft.Extensions.Configuration;
using RecipeApp.Application.Common.Interface;
using Resend;

namespace RecipeApp.Infrastructure.Common;

public class EmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IResend resend, IConfiguration config)
    {
        _resend = resend;
        _fromEmail = config["Resend:FromEmail"]!;
        _fromName = config["Resend:FromName"]!;
    }

    public async Task SendPasswordResetCodeAsync(string toEmail, string userName, string code)
    {
        var message = new EmailMessage
        {
            From = $"{_fromName} <{_fromEmail}>",
            Subject = "Código para redefinir sua senha",
            HtmlBody = $@"
                <div style=""font-family: sans-serif; max-width: 480px; margin: 0 auto;"">
                    <h2>Redefinição de senha</h2>
                    <p>Olá, {userName}!</p>
                    <p>Use o código abaixo para redefinir sua senha no RecipeApp. Ele expira em 15 minutos.</p>
                    <p style=""font-size: 32px; font-weight: bold; letter-spacing: 4px; text-align: center; padding: 16px; background: #f4f4f5; border-radius: 8px;"">{code}</p>
                    <p>Se você não pediu essa alteração, pode ignorar este email.</p>
                </div>"
        };
        message.To.Add(toEmail);

        await _resend.EmailSendAsync(message);
    }
}