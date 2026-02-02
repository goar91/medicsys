using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;

namespace MEDICSYS.Api.Services;

public class ReminderWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ReminderWorker> _logger;

    public ReminderWorker(IServiceProvider services, ILogger<ReminderWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow;
                var due = await db.Reminders
                    .Where(r => r.Status == "Pending" && r.ScheduledAt <= now)
                    .ToListAsync(stoppingToken);

                if (due.Count > 0)
                {
                    foreach (var reminder in due)
                    {
                        reminder.Status = "Due";
                    }
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReminderWorker failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
