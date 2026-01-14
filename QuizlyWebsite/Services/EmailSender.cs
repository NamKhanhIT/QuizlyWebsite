using System.Net;
using System.Net.Mail;

namespace QuizlyWebsite.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(
            string to,
            string subject,
            string body)
        {
            try
            {
                // Validate email address
                if (string.IsNullOrWhiteSpace(to))
                {
                    _logger.LogError("Email recipient address is empty.");
                    return false;
                }

                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
                var smtpUsername = _configuration["Email:SmtpUsername"];
                var smtpPassword = _configuration["Email:SmtpPassword"];

                if (string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword))
                {
                    _logger.LogError("SMTP credentials not configured. Please check appsettings.json");
                    return false;
                }

                var fromEmail = _configuration["Email:FromEmail"] ?? smtpUsername;
                var fromName = _configuration["Email:FromName"] ?? "Quizly Website";

                _logger.LogInformation($"Attempting to send email to {to} from {fromEmail} via {smtpHost}:{smtpPort}");

                using var client = new SmtpClient(smtpHost, smtpPort);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                client.Timeout = 20000;

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(to);

                await client.SendMailAsync(message);
                _logger.LogInformation($"Email sent successfully to {to}");
                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error sending email to {To}. StatusCode: {StatusCode}", to, smtpEx.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {To}. Error: {Message}", to, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendEmailWithAttachmentAsync(
            string to,
            string subject,
            string body,
            byte[] attachment,
            string fileName)
        {
            try
            {
                // Validate email address
                if (string.IsNullOrWhiteSpace(to))
                {
                    _logger.LogError("Email recipient address is empty.");
                    return false;
                }

                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
                var smtpUsername = _configuration["Email:SmtpUsername"];
                var smtpPassword = _configuration["Email:SmtpPassword"];

                if (string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword))
                {
                    _logger.LogError("SMTP credentials not configured. Please check appsettings.json");
                    return false;
                }

                var fromEmail = _configuration["Email:FromEmail"] ?? smtpUsername;
                var fromName = _configuration["Email:FromName"] ?? "Quizly Website";

                _logger.LogInformation($"Attempting to send email to {to} from {fromEmail} via {smtpHost}:{smtpPort}");

                using var client = new SmtpClient(smtpHost, smtpPort);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                client.Timeout = 20000;

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(to);

                if (attachment != null && attachment.Length > 0)
                {
                    var stream = new MemoryStream(attachment);
                    stream.Position = 0; 
                    var attachmentItem = new Attachment(stream, fileName)
                    {
                        ContentType = new System.Net.Mime.ContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                    };
                    message.Attachments.Add(attachmentItem);
                }

                await client.SendMailAsync(message);
                _logger.LogInformation($"Email sent successfully to {to}");
                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error sending email to {To}. StatusCode: {StatusCode}", to, smtpEx.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {To}. Error: {Message}", to, ex.Message);
                return false;
            }
        }
    }
}
