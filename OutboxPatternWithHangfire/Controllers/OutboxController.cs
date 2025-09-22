using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutboxPatternWithHangfire.Data;

namespace OutboxPatternWithHangfire.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OutboxController : ControllerBase
    {
        private readonly AppDbContext _db;
        public OutboxController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetOutboxMessages()
        {
            var messages = await _db.OutboxMessages.OrderByDescending(x => x.OccurredOn).ToListAsync();
            return Ok(messages);
        }
    }
}