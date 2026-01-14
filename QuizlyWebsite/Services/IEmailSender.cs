public interface IEmailSender
{
    Task<bool> SendEmailWithAttachmentAsync(
        string to,
        string subject,
        string body,
        byte[] attachment,
        string fileName
    );

    Task<bool> SendEmailAsync(
        string to,
        string subject,
        string body
    );
}
