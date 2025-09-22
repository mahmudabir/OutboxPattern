using Microsoft.EntityFrameworkCore;
using OutboxPatternWithHangfire.Models;

namespace OutboxPatternWithHangfire.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    }
}