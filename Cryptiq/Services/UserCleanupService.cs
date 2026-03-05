using CryptiqChat.Data;
using Microsoft.EntityFrameworkCore;

public class UserCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public UserCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CryptiqDbContext>();

            var cutoff = DateTime.UtcNow.AddDays(-30);

            var usersToDelete = await db.Users
                .Where(u => u.StatusId == 2 && u.DateOfRegistration < cutoff) 
                .ToListAsync();

            if (usersToDelete.Any())
            {
                db.Users.RemoveRange(usersToDelete);
                await db.SaveChangesAsync();
            }

            // Espera 24 horas antes de volver a revisar
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
