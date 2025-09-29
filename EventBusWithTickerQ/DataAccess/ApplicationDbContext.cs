using Microsoft.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore.Configurations;

namespace EventBusWithTickerQ.DataAccess
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply TickerQ entity configurations explicitly
            // Default Schema is "ticker".
            modelBuilder.ApplyConfiguration(new TimeTickerConfigurations());
            modelBuilder.ApplyConfiguration(new CronTickerConfigurations());
            modelBuilder.ApplyConfiguration(new CronTickerOccurrenceConfigurations());
        }
    }

}
