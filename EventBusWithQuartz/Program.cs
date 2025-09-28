using EventBusWithQuartz.Abstractions;
using EventBusWithQuartz.DataAccess;
using EventBusWithQuartz.EventHandlers;
using EventBusWithQuartz.Events;
using EventBusWithQuartz.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Impl.AdoJobStore;
using System.Configuration;

namespace EventBusWithQuartz
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen();

            var quartzConnection = builder.Configuration.GetConnectionString("Quartz");

            builder.Services.AddQuartz(q =>
            {
                // MicrosoftDependencyInjectionJobFactory is default (no need to call obsolete method)
                q.UseSimpleTypeLoader();
                q.UsePersistentStore(c =>
                {
                    c.RetryInterval = TimeSpan.FromMinutes(1);
                    c.UseProperties = true;
                    c.PerformSchemaValidation = false;
                    c.UseSystemTextJsonSerializer();

                    c.UseSqlServer(store =>
                    {
                        store.ConnectionString = quartzConnection!;
                        // Include schema in table prefix so Quartz queries quartz.QRTZ_* tables
                        store.TablePrefix = "quartz.QRTZ_"; 
                        store.UseDriverDelegate<SqlServerDelegate>();
                    });
                });

                q.UseDefaultThreadPool(tp => tp.MaxConcurrency = Math.Max(Environment.ProcessorCount, 4));
            });
            builder.Services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
                options.AwaitApplicationStarted = true;
            });

            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(quartzConnection));

            builder.Services.AddScoped<IEventBus, QuartzEventBus>();
            builder.Services.AddScoped<IIntegrationEventHandler<OrderCreatedEvent>, SendEmailOnOrderCreatedHandler>();
            builder.Services.AddScoped<IIntegrationEventHandler<OrderCreatedEvent>, UpdateReadModelOnOrderCreatedHandler>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers();

            using (var scope = app.Services.CreateScope())
            {
                var schedulerFactory = scope.ServiceProvider.GetRequiredService<ISchedulerFactory>();
                var scheduler = await schedulerFactory.GetScheduler();
                await RecurringJobs.RegisterAsync(scheduler);
            }

            await app.RunAsync();
        }
    }
}
