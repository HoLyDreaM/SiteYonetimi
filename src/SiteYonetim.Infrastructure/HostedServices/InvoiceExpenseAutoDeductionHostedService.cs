using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.Infrastructure.HostedServices;

/// <summary>
/// Her gün çalışır; fatura tarihi gelmiş ancak henüz bankadan düşülmemiş giderleri
/// varsayılan banka hesabından otomatik olarak düşer.
/// </summary>
public class InvoiceExpenseAutoDeductionHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InvoiceExpenseAutoDeductionHostedService> _logger;

    public InvoiceExpenseAutoDeductionHostedService(IServiceProvider services, ILogger<InvoiceExpenseAutoDeductionHostedService> logger)
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
                var service = scope.ServiceProvider.GetRequiredService<IInvoiceExpenseAutoDeductionService>();
                await service.ProcessDueExpensesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatura gider otomatik düşüm hatası");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
