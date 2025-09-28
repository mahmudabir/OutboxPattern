using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutboxPatternWithQuartz.Data;

namespace OutboxPatternWithQuartz.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OutboxController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet]
    public async Task<IActionResult> GetOutboxMessages()
    {
        var messages = await _db.OutboxMessages.OrderByDescending(x => x.OccurredOn).ToListAsync();
        return Ok(messages);
    }
}
