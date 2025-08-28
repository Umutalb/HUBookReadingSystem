using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HUBookReadingSystem.Data;
using HUBookReadingSystem.Models;
using HUBookReadingSystem.Dtos;
using Microsoft.Extensions.Logging;

namespace HUBookReadingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReadingItemsController : AppControllerBase
    {
        private readonly ILogger<ReadingItemsController> _logger;

        public ReadingItemsController(AppDbContext context, ILogger<ReadingItemsController> logger) : base(context) 
        { 
            _logger = logger;
        }

        [HttpPost("{readerId}")]
        public async Task<IActionResult> AddItem(int readerId, [FromBody] CreateItemRequest item)
        {
            if (item == null)
                return BadRequest();

            var currentReaderId = await GetCurrentReaderId();
            if (currentReaderId == null)
            {
                _logger.LogWarning("Unauthorized attempt to add item for reader {ReaderId}", readerId);
                return Unauthorized();
            }

            if (currentReaderId.Value != readerId)
            {
                _logger.LogWarning("User {UserId} attempted to add item for reader {ReaderId} without permission", 
                    currentReaderId.Value, readerId);
                return Forbid();
            }

            var reader = await _context.Readers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == readerId);

            if (reader == null)
            {
                _logger.LogWarning("Reader with ID {ReaderId} not found for adding item", readerId);
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(item.Title))
                return BadRequest("Title is required!");

            if (item.Title.Trim().Length > 200)
                return BadRequest("Title max 200 characters!");

            var newItem = new ReadingItem
            {
                ReaderId = reader.Id,
                Round = reader.CurrentRound,
                Title = item.Title.Trim(),
                FinishedAt = null,
            };

            if (item.StartedAt.HasValue)
                newItem.StartedAt = item.StartedAt.Value.Date;

            await _context.ReadingItems.AddAsync(newItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully added new reading item '{Title}' for reader {ReaderId}", 
                item.Title.Trim(), readerId);

            return StatusCode(201, new
            {
                newItem.Id,
                newItem.ReaderId,
                newItem.Title,
                newItem.Round,
                newItem.IsDone,
                newItem.StartedAt,
                newItem.FinishedAt,
                newItem.CreatedAt
            });
        }

        [HttpPatch("{readerId}/{itemId}")]
        public async Task<IActionResult> ToggleDone(int readerId, int itemId)
        {
            var currentReaderId = await GetCurrentReaderId();
            if (currentReaderId == null)
            {
                _logger.LogWarning("Unauthorized attempt to toggle item {ItemId} for reader {ReaderId}", itemId, readerId);
                return Unauthorized();
            }

            if (currentReaderId.Value != readerId)
            {
                _logger.LogWarning("User {UserId} attempted to toggle item {ItemId} for reader {ReaderId} without permission", 
                    currentReaderId.Value, itemId, readerId);
                return Forbid();
            }

            var reader = await _context.Readers
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == readerId);

            if (reader == null)
            {
                _logger.LogWarning("Reader with ID {ReaderId} not found for toggling item", readerId);
                return NotFound();
            }

            var item = await _context.ReadingItems
                .FirstOrDefaultAsync(x => x.Id == itemId && x.ReaderId == readerId);

            if (item == null)
            {
                _logger.LogWarning("Reading item {ItemId} not found for reader {ReaderId}", itemId, readerId);
                return NotFound();
            }

            if (item.Round < reader.CurrentRound)
            {
                _logger.LogWarning("Attempt to toggle history item {ItemId} for reader {ReaderId}", itemId, readerId);
                return BadRequest("History (previous rounds) items cannot be toggled");
            }

            if (!item.IsDone)
            {
                item.IsDone = true;
                item.FinishedAt = DateTime.UtcNow;
                _logger.LogInformation("Marked reading item '{Title}' as done for reader {ReaderId}", item.Title, readerId);
            }
            else
            {
                item.IsDone = false;
                item.FinishedAt = null;
                _logger.LogInformation("Marked reading item '{Title}' as not done for reader {ReaderId}", item.Title, readerId);
            }
            
            await _context.SaveChangesAsync();

            return Ok(new { item.Id, item.IsDone, item.FinishedAt });
        }

        [HttpDelete("{readerId}/{itemId}")]
        public async Task<IActionResult> DeleteItem(int readerId, int itemId)
        {
            var currentReaderId = await GetCurrentReaderId();
            if (currentReaderId == null)
            {
                _logger.LogWarning("Unauthorized attempt to delete item {ItemId} for reader {ReaderId}", itemId, readerId);
                return Unauthorized();
            }

            if (currentReaderId.Value != readerId)
            {
                _logger.LogWarning("User {UserId} attempted to delete item {ItemId} for reader {ReaderId} without permission", 
                    currentReaderId.Value, itemId, readerId);
                return Forbid();
            }

            var reader = await _context.Readers
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == readerId);

            if (reader == null)
            {
                _logger.LogWarning("Reader with ID {ReaderId} not found for deleting item", readerId);
                return NotFound();
            }

            var item = await _context.ReadingItems
                .FirstOrDefaultAsync(x => x.Id == itemId && x.ReaderId == readerId);

            if (item == null)
            {
                _logger.LogWarning("Reading item {ItemId} not found for reader {ReaderId}", itemId, readerId);
                return NotFound();
            }

            if (item.Round < reader.CurrentRound)
            {
                _logger.LogWarning("Attempt to delete history item {ItemId} for reader {ReaderId}", itemId, readerId);
                return BadRequest("History (previous rounds) items cannot be deleted");
            }

            _context.ReadingItems.Remove(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted reading item '{Title}' for reader {ReaderId}", item.Title, readerId);

            return NoContent();
        }

        [HttpPatch("{readerId}/{itemId}/edit")]
        public async Task<IActionResult> UpdateItem(int readerId, int itemId, [FromBody] UpdateItemRequest req)
        {
            if (req == null) return BadRequest();

            var currentReaderId = await GetCurrentReaderId();
            if (currentReaderId == null) return Unauthorized();
            if (currentReaderId.Value != readerId) return Forbid();

            var reader = await _context.Readers
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == readerId);
            if (reader == null) return NotFound();

            
            var item = await _context.ReadingItems
                .FirstOrDefaultAsync(x => x.Id == itemId && x.ReaderId == readerId);
            if (item == null) return NotFound();

            if (req.Title == null && !req.StartedAt.HasValue)
                return BadRequest("Nothing to update");

            
            if (item.Round != reader.CurrentRound && req.StartedAt.HasValue)
                return BadRequest("Only items in the current round can change StartedAt");

            
            if (req.Title != null)
            {
                var t = req.Title.Trim();
                if (t.Length == 0) return BadRequest("Title is required");
                if (t.Length > 200) return BadRequest("Title max 200 characters!");
                item.Title = t;
            }

           
            if (req.StartedAt.HasValue)
            {
                var d = req.StartedAt.Value.Date;
                item.StartedAt = DateTime.SpecifyKind(d, DateTimeKind.Utc);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                item.Id,
                item.ReaderId,
                item.Title,
                item.Round,
                item.IsDone,
                item.StartedAt,
                item.FinishedAt,
                item.CreatedAt
            });
        }
    }
}
