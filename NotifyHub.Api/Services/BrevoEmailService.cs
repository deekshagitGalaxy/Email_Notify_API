using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NotifyHub.Api.Services
{
    public class BrevoEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<BrevoEmailService> _logger;

        public BrevoEmailService(IConfiguration config, ILogger<BrevoEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendBookingConfirmationAsync(string toEmail, string customerName, string serviceName, DateTime bookingDateTime)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));

            message.To.Add(new MailboxAddress(customerName, toEmail));
            message.Subject = "Booking Confirmation - NotifyHub";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <p>Hi <strong>{customerName}</strong>,</p>
                    <p>Your booking for <strong>{serviceName}</strong> is confirmed.</p>
                    <p><strong>Date & Time:</strong> {bookingDateTime:dddd, MMMM d, yyyy h:mm tt}</p>
                    <p>Thank you for choosing NotifyHub!</p>"
            };

            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(
                    _config["EmailSettings:SmtpHost"]!,
                    int.Parse(_config["EmailSettings:SmtpPort"]!),
                    SecureSocketOptions.StartTls);

                await client.AuthenticateAsync(
                    _config["EmailSettings:SmtpUsername"]!,
                    _config["EmailSettings:SmtpPassword"]!);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Confirmation email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email to {Email}", toEmail);
                throw;
            }
        }
    }
}