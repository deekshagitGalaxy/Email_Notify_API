using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotifyHub.Api.Data;
using NotifyHub.Api.Models;
using NotifyHub.Api.Services;

namespace NotifyHub.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(AppDbContext context, IEmailService emailService, ILogger<BookingsController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<Booking>>> GetBookings()
        {
            var bookings = await _context.Bookings.ToListAsync();
            return Ok(bookings);
        }

        [HttpPost]
        public async Task<ActionResult<Booking>> CreateBooking(Booking booking)
        {
            booking.CreatedAt = DateTime.UtcNow;
            booking.ConfirmationEmailSent = false;

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Try to send the confirmation email — but don't let email failure break the booking itself
            try
            {
                await _emailService.SendBookingConfirmationAsync(
                    booking.CustomerEmail,
                    booking.CustomerName,
                    booking.ServiceName,
                    booking.BookingDateTime);

                booking.ConfirmationEmailSent = true;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Booking {Id} saved but confirmation email failed", booking.Id);
                // We don't throw here — the booking itself succeeded, only the email failed
            }

            return CreatedAtAction(nameof(GetBookings), new { id = booking.Id }, booking);
        }
    }
}