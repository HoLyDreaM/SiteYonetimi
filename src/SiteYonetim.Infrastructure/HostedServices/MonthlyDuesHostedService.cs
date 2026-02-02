using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.Infrastructure.HostedServices;

/// <summary>
/// Her gün çalışır; ayın 1'i ise tüm sitelerdeki daireler için o ayın aidat (Income) kayıtlarını oluşturur.
/// </summary>
public class MonthlyDuesHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MonthlyDuesHostedService> _logger;

    public MonthlyDuesHostedService(IServiceProvider services, ILogger<MonthlyDuesHostedService> logger)
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
                var now = DateTime.UtcNow;
                if (now.Day == 1)
                {
                    using var scope = _services.CreateScope();
                    var incomeService = scope.ServiceProvider.GetRequiredService<IIncomeService>();
                    var year = now.Year;
                    var month = now.Month;
                    await incomeService.EnsureMonthlyIncomesAsync(year, month, stoppingToken);
                    _logger.LogInformation("Aylık aidat kayıtları oluşturuldu: {Year}-{Month}", year, month);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aylık aidat oluşturma hatası");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Her saat kontrol
        }
    }
}
