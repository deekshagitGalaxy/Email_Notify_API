# NotifyHub

A learning project built to understand how a real backend sends automated emails — specifically, a **booking confirmation system** built with ASP.NET Core, EF Core, and Brevo (SMTP email provider).

## What this project actually does

1. A booking is created (currently tested via Swagger)
2. The booking is saved to a SQL Server database using EF Core
3. Immediately after saving, a confirmation email is sent automatically via Brevo's SMTP relay
4. If the email fails for any reason, the booking is **still saved successfully** — only the email send is marked as failed, so it can be retried later without losing data

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core Web API (.NET 9) |
| Database | SQL Server (local instance, via SSMS) |
| ORM | Entity Framework Core 9.0.0 |
| Email sending | MailKit (SMTP client library) |
| Email provider | Brevo (SMTP relay) |
| API testing | Swagger UI (Swashbuckle.AspNetCore) |

## Project Structure

```
NotifyHub.Api/
├── Controllers/
│   └── BookingsController.cs       # API endpoints: GET/POST /api/bookings
├── Models/
│   └── Booking.cs                  # The Booking entity (maps to the Bookings table)
├── Data/
│   └── AppDbContext.cs             # EF Core's bridge between C# and SQL Server
├── Services/
│   ├── IEmailService.cs            # Email-sending abstraction (interface)
│   └── BrevoEmailService.cs        # Brevo/MailKit implementation of IEmailService
├── Migrations/                     # EF Core migration history (auto-generated)
├── Program.cs                      # App startup: DI registration, middleware pipeline
└── appsettings.json                # Connection string + email settings
```

## Core Concepts Learned

### 1. Entity Framework Core (ORM)
EF Core maps C# classes to database tables, so SQL doesn't need to be written by hand.
- `Booking.cs` → defines the shape of one row of data (an **entity**)
- `AppDbContext.cs` → manages the connection and exposes `DbSet<Booking> Bookings`
- `dotnet ef migrations add` → generates the SQL needed to create/update tables based on the C# model
- `dotnet ef database update` → actually runs that SQL against the real database
- `_context.Bookings.Add(...)` + `SaveChangesAsync()` → EF Core generates and executes the actual `INSERT` statement

### 2. Dependency Injection
`BookingsController` doesn't create its own `AppDbContext` or `IEmailService` — ASP.NET Core's built-in DI container creates and injects them automatically, based on what's registered in `Program.cs`:
```csharp
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddScoped<IEmailService, BrevoEmailService>();
```

### 3. Interface-based abstraction for email
`BookingsController` depends on `IEmailService` (the contract), not `BrevoEmailService` (the concrete implementation) directly. This means the email provider could be swapped (e.g., to SendGrid or Resend) by writing one new class — no changes needed anywhere else in the app.

### 4. SMTP
- **SMTP** = Simple Mail Transfer Protocol — the protocol used to *send* email (receiving uses a different protocol, IMAP/POP3)
- Brevo gives a **separate SMTP login** (e.g. `xxxxx@smtp-brevo.com`) that is different from the account's actual email address — this was a real issue hit during setup (`535: Authentication failed` until this was corrected)
- Port `587` with `StartTls` is used for encrypted submission

### 5. Defensive error handling around third-party dependencies
Email sending is wrapped in its own try/catch, separate from the database save:
```csharp
_context.Bookings.Add(booking);
await _context.SaveChangesAsync();   // booking is saved regardless of what happens next

try
{
    await _emailService.SendBookingConfirmationAsync(...);
    booking.ConfirmationEmailSent = true;
    await _context.SaveChangesAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Booking {Id} saved but confirmation email failed", booking.Id);
    // booking is NOT rolled back — only the email failed
}
```
This means if Brevo is down, or an API key is revoked, the core business action (the booking) still succeeds — only the email needs retrying. The `ConfirmationEmailSent` boolean flag on `Booking` exists specifically so failed sends could be queried and retried later.


## Known TODO / Not Yet Done

- [ ] Angular frontend (booking form UI) — backend currently tested via Swagger only
- [ ] Authentication (login/JWT) — intentionally skipped in v1 to focus on the core DB + email flow first
- [ ] Background job for scheduled reminder emails (e.g., via Hangfire or `IHostedService`) — planned as a "v2" feature once the core flow is solid

## Quick Reference: Running This Project

```bash
# Restore & build
dotnet build

# Apply database migrations (creates DB + tables if they don't exist)
dotnet ef database update

# Run the API
dotnet run

# Open Swagger UI to test endpoints
# http://localhost:5160/swagger
```

- Integrating a real third-party service (Brevo) via SMTP
- Designing for failure: a third-party dependency (email) failing does **not** break the core business operation (the booking)
- Abstracting external dependencies behind interfaces for swappability and testability
