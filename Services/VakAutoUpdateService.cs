using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChineseAcademicPortal.Services;

public class VakAutoUpdateService : IHostedService, IDisposable
{
    private readonly ILogger<VakAutoUpdateService> _logger;
    private readonly IServiceProvider _services;
    private Timer _timer;

    // Запуск: сразу при старте + раз в 7 дней (604800000 мс)
    private const int UpdateInterval = 7 * 24 * 60 * 60 * 1000;

    public VakAutoUpdateService(
        ILogger<VakAutoUpdateService> logger,
        IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔄 VAK auto-update service starting");

        // Запускаем сразу + по расписанию
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(UpdateInterval));

        return Task.CompletedTask;
    }

    private async void DoWork(object state)
    {
        try
        {
            _logger.LogInformation("🚀 Starting scheduled VAK import...");

            using var scope = _services.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<VakJsonImportService>();

            var count = await importService.ImportFromJsonAsync();

            _logger.LogInformation("✅ Scheduled import completed: {Count} journals updated", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Scheduled VAK import failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 VAK auto-update service stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}