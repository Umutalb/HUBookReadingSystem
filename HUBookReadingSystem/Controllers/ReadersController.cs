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
    public class ReadersController : AppControllerBase
    {
        private readonly ILogger<ReadersController> _logger;

        public ReadersController(AppDbContext context, ILogger<ReadersController> logger) : base(context) 
        { 
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var readers = await _context.Readers
                .AsNoTracking()
               .Select(r => new
               {
                   r.Id,
                   r.Name,
                   r.TargetCount,
                   r.CurrentRound
               })
                    .ToListAsync();

            return Ok(readers);
        }

        [HttpGet("{id}/items")]
        public async Task<IActionResult> GetReaderItems(int id)
        {
            var reader = await _context.Readers.FindAsync(id);

            if (reader == null)
            {
                _logger.LogWarning("Reader with ID {ReaderId} not found", id);
                return NotFound();
            }

            var items = await _context.ReadingItems
                .AsNoTracking()
                .Where(x => x.ReaderId == id && x.Round == reader.CurrentRound)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.IsDone,
                    x.Round,
                    x.StartedAt,
                    x.FinishedAt,
                    x.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var reader = await _context.Readers
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.TargetCount,
                    r.CurrentRound,
                    r.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (reader == null)
            {
                _logger.LogWarning("Reader with ID {ReaderId} not found", id);
                return NotFound();
            }

            return Ok(reader);
        }

        [HttpGet("{id}/stats")]
        public async Task<IActionResult> GetStats(int id)
        {
            var reader = await _context.Readers.FindAsync(id);

            if (reader == null)
            {
                _logger.LogWarning("Reader with ID {ReaderId} not found for stats", id);
                return NotFound();
            }

            var done = await _context.ReadingItems
                .AsNoTracking()
                .Where(x => x.ReaderId == id && x.Round == reader.CurrentRound && x.IsDone)
                .CountAsync();

            var target = reader.TargetCount < 0 ? 0 : reader.TargetCount;
            var remaining = target > done ? target - done : 0;
            var progressPct = target > 0 ? Math.Min(100, (int)Math.Floor(done * 100.0 / target)) : 0;

            var result = new
            {
                readerId = reader.Id,
                target,
                done,
                remaining,
                progressPct,
                currentRound = reader.CurrentRound,
            };

            return Ok(result);
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistory(int id, [FromQuery] bool onlyDone = true)
        {
            var reader = await _context.Readers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (reader == null)
            {
                _logger.LogWarning("Reader with ID {ReaderId} not found for history", id);
                return NotFound();
            }

            var q = _context.ReadingItems
                .AsNoTracking()
                .Where(x => x.ReaderId == id && x.Round < reader.CurrentRound);

            if (onlyDone)
                q = q.Where(x => x.IsDone);

            q = q.OrderByDescending(x => x.FinishedAt);

            var history = await q
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.IsDone,
                    x.Round,
                    x.StartedAt,
                    x.FinishedAt,
                    x.CreatedAt
                })
                .ToListAsync();

            return Ok(history);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReaderRequest req)
        {
            if (req == null)
                return BadRequest();

            var readerId = await GetCurrentReaderId();
            if (readerId == null)
            {
                _logger.LogWarning("Unauthorized update attempt for reader {ReaderId}", id);
                return Unauthorized();
            }

            if (readerId.Value != id)
            {
                _logger.LogWarning("User {UserId} attempted to update reader {ReaderId} without permission", 
                    readerId.Value, id);
                return Forbid();
            }

            var reader = await _context.Readers.FindAsync(id);

            if (reader == null)
            {
                _logger.LogWarning("Reader with ID {ReaderId} not found for update", id);
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(req.Name))
                reader.Name = req.Name;

            if (req.TargetCount.HasValue)
            {
                if (req.TargetCount.Value < 0)
                {
                    _logger.LogWarning("Invalid TargetCount {TargetCount} for reader {ReaderId}", 
                        req.TargetCount.Value, id);
                    return BadRequest("TargetCount cannot be less than 0");
                }

                reader.TargetCount = req.TargetCount.Value;
            }

            if (req.CurrentRound.HasValue)
            {
                if (req.CurrentRound.Value < 1)
                {
                    _logger.LogWarning("Invalid CurrentRound {CurrentRound} for reader {ReaderId}", 
                        req.CurrentRound.Value, id);
                    return BadRequest("CurrentRound cannot be less than 1");
                }

                reader.CurrentRound = req.CurrentRound.Value;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully updated reader {ReaderId}", id);

            return Ok(new
            {
                reader.Id,
                reader.Name,
                reader.TargetCount,
                reader.CurrentRound,
                reader.CreatedAt
            });
        }

        [HttpPatch("{id}/round")]
        public async Task<IActionResult> IncreaseRound(int id, [FromBody] RoundRequest req)
        {
            if (req == null || !req.Confirm)
            {
                _logger.LogWarning("Round increase attempt without confirmation for reader {ReaderId}", id);
                return BadRequest("Confirmation required to increase round.");
            }

            var readerId = await GetCurrentReaderId();
            if (readerId == null)
            {
                _logger.LogWarning("Unauthorized round increase attempt for reader {ReaderId}", id);
                return Unauthorized();
            }

            if (readerId.Value != id)
            {
                _logger.LogWarning("User {UserId} attempted to increase round for reader {ReaderId} without permission", 
                    readerId.Value, id);
                return Forbid();
            }

            var reader = await _context.Readers.FirstOrDefaultAsync(r => r.Id == id);

            if (reader == null)
            {
                _logger.LogWarning("Reader with ID {ReaderId} not found for round increase", id);
                return NotFound();
            }

            var oldRound = reader.CurrentRound;
            reader.CurrentRound += 1;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully increased round for reader {ReaderId} from {OldRound} to {NewRound}", 
                id, oldRound, reader.CurrentRound);

            return Ok(new { reader.Id, reader.CurrentRound });
        }
    }
}

