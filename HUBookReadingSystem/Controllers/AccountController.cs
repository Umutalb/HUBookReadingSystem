using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HUBookReadingSystem.Data;
using HUBookReadingSystem.Models;
using HUBookReadingSystem.Dtos;
using HUBookReadingSystem.Security;

namespace HUBookReadingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : AppControllerBase
    {
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext context, ILogger<AccountController> logger) : base(context) 
        { 
            _logger = logger;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var name = req?.Name?.Trim();
            var pin = req?.Pin?.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(pin))
            {
                _logger.LogWarning("Login attempt with missing name or PIN");
                return BadRequest("Name and PIN are required");
            }

            if (pin.Length < 4 || pin.Length > 6)
            {
                _logger.LogWarning("Login attempt with invalid PIN length for user: {Name}", name);
                return BadRequest("PIN must be 4-6 digits");
            }

            var reader = await _context.Readers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == name);

            if (reader == null)
            {
                _logger.LogWarning("Failed login attempt for user: {Name}", name);
                await Task.Delay(500);
                return Unauthorized("Invalid credentials!");
            }

            var ok = Pbkdf2.Verify(pin, reader.PinSalt, reader.PinHash);
            if (!ok)
            {
                await Task.Delay(500);
                return Unauthorized("Invalid credentials!");
            }

            var session = new Sessions
            {
                Id = Guid.NewGuid(),
                ReaderId = reader.Id,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                RevokedAtUtc = null
            };
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            Response.Cookies.Append(
                CookieName,
                session.Id.ToString(),
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    MaxAge = TimeSpan.FromDays(30),
                    Path = "/"
                });

            _logger.LogInformation("Successful login for user: {Name} (ID: {ReaderId})", name, reader.Id);

            return Ok(new
            {
                reader.Id,
                reader.Name,
                reader.TargetCount,
                reader.CurrentRound
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var cookie = Request.Cookies[CookieName];
            if (string.IsNullOrEmpty(cookie))
                return NoContent(); 


            if (!Guid.TryParse(cookie, out var sessionId))
            {
                _logger.LogWarning("Logout attempt with invalid session cookie format");
                return NoContent();
            }

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.RevokedAtUtc == null);

            if (session == null)
            {
                _logger.LogInformation("Logout for non-active or non-existent session: {SessionId}", sessionId);
            }
            else
            {
                session.RevokedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully logged out session: {SessionId}", sessionId);
            }

            Response.Cookies.Delete(CookieName, new CookieOptions
            {
                Path = "/",
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.None
            });

            return NoContent();
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var readerId = await GetCurrentReaderId();
            if (readerId == null)
            {
                _logger.LogWarning("Unauthorized request to get current user");
                return Unauthorized();
            }

            var reader = await _context.Readers
                .AsNoTracking()
                .Where(r => r.Id == readerId.Value)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.TargetCount,
                    r.CurrentRound
                })
                .FirstOrDefaultAsync();

            if (reader == null)
            {
                _logger.LogWarning("Current user not found in database: {ReaderId}", readerId.Value);
                return Unauthorized();
            }

            return Ok(reader);
        }
    }
}
