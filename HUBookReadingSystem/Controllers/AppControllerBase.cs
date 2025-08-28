using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HUBookReadingSystem.Data;
using HUBookReadingSystem.Models;

namespace HUBookReadingSystem.Controllers
{
    public abstract class AppControllerBase : ControllerBase
    {
        protected readonly AppDbContext _context;
        protected const string CookieName = "hu_session";

        protected AppControllerBase(AppDbContext context)
        {
            _context = context;
        }

        protected async Task<int?> GetCurrentReaderId()
        {
            var cookie = Request.Cookies[CookieName];
            if (string.IsNullOrEmpty(cookie))
                return null;

            if (!Guid.TryParse(cookie, out var sessionId))
                return null;

            var session = await _context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync( s =>
                s.Id == sessionId &&
                s.RevokedAtUtc == null &&
                s.ExpiresAtUtc > DateTime.UtcNow
                );

            return session?.ReaderId;       
        }
    }
}
