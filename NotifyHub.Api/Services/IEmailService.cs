namespace NotifyHub.Api.Services
{
    public interface IEmailService
    {
        Task SendBookingConfirmationAsync(string toEmail, string customerName, string serviceName, DateTime bookingDateTime);
    }
}