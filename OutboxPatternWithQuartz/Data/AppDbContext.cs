using Microsoft.EntityFrameworkCore;
using OutboxPatternWithQuartz.Models;

namespace OutboxPatternWithQuartz.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
}
