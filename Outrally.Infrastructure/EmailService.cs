using Resend;

namespace Outrally.Infrastructure;

public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string resetLink);
}

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;

    public ResendEmailService(IResend resend)
    {
        _resend = resend;
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        var message = new EmailMessage
        {
            From = "noreply@juanmunoz.dev",
            To = { toEmail },
            Subject = "Reset your Outrally password",
            HtmlBody = $"""
                <h2>Reset your Outrally password</h2>
                <p>Click the link below to reset your password. This link expires in 1 hour.</p>
                <a href="{resetLink}">Reset Password</a>
                <p>If you didn't request this, ignore this email.</p>
                """
        };
        await _resend.EmailSendAsync(message);
    }
}