using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace EventBusWithQuartz.DataAccess
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Adds Quartz.NET PostgreSQL schema to EntityFrameworkCore
            modelBuilder.AddQuartz(options => options.UseSqlServer());
        }
    }

}
