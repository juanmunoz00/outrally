using Resend;

namespace Outrally.Infrastructure;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string firstName, string tempPassword);
    Task SendTempPasswordEmailAsync(string toEmail, string firstName, string tempPassword);
}

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;

    public ResendEmailService(IResend resend)
    {
        _resend = resend;
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string firstName, string tempPassword)
    {
        var message = new EmailMessage
        {
            From = "noreply@juanmunoz.dev",
            To = { toEmail },
            Subject = "Welcome to Outrally!",
            HtmlBody = $"""
                <h2>Welcome to Outrally, {firstName}!</h2>
                <p>Your account has been created. Use the temporary password below to sign in.</p>
                <p style="font-size:1.5rem;font-weight:bold;letter-spacing:2px">{tempPassword}</p>
                <p>You will be asked to change it on your first login.</p>
                """
        };
        await _resend.EmailSendAsync(message);
    }

    public async Task SendTempPasswordEmailAsync(string toEmail, string firstName, string tempPassword)
    {
        var message = new EmailMessage
        {
            From = "noreply@juanmunoz.dev",
            To = { toEmail },
            Subject = "Your Outrally password reset",
            HtmlBody = $"""
                <h2>Hi {firstName},</h2>
                <p>Here is your temporary password:</p>
                <p style="font-size:1.5rem;font-weight:bold;letter-spacing:2px">{tempPassword}</p>
                <p>Sign in and you will be prompted to set a new password.</p>
                """
        };
        await _resend.EmailSendAsync(message);
    }
}