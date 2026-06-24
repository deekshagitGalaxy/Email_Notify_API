namespace NotifyHub.Api.Models
{
	public class Booking
	{
		public int Id { get; set; }                  // Primary key (auto-incremented by DB)
		public string CustomerName { get; set; } = string.Empty;
		public string CustomerEmail { get; set; } = string.Empty;
		public string ServiceName { get; set; } = string.Empty;   // e.g. "Haircut", "Consultation"
		public DateTime BookingDateTime { get; set; }             // The actual appointment date/time
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // When the booking record was created
		public bool ConfirmationEmailSent { get; set; } = false;   // Tracks if email succeeded — useful for debugging later
	}
}