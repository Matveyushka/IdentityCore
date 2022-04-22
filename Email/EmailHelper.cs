using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IdentityCore.Email
{
    public class EmailHelper : IEmailSender
    {
        ILogger _logger;
        string emailLogin = Startup.Configuration.GetValue<string>("EmailLogin");
        string emailPassword = Startup.Configuration.GetValue<string>("EmailPassword");

        public EmailHelper(ILogger<EmailHelper> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(emailLogin);
            mailMessage.To.Add(new MailAddress(email));

            mailMessage.Subject = subject;
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = htmlMessage;

            SmtpClient client = new SmtpClient();
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Timeout = 10000;
            client.Credentials = new System.Net.NetworkCredential(emailLogin, emailPassword);
            client.Host = "smtp.gmail.com";
            client.Port = 587;
            try
            {
                client.Send(mailMessage);
                _logger.LogInformation("Email sent");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Email sending error: {ex.Message}");
            }
            return Task.Run(() => { });
        }
    }
}