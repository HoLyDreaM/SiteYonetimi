using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;
using SiteYonetim.Infrastructure.Services;

namespace SiteYonetim.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SiteYonetimDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                b =>
                {
                    b.MigrationsAssembly(typeof(SiteYonetimDbContext).Assembly.FullName);
                    b.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                }));

        services.AddScoped<ISiteService, SiteService>();
        services.AddScoped<IApartmentService, ApartmentService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IExpenseShareService, ExpenseShareService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IMeterService, MeterService>();
        services.AddScoped<IExpenseTypeService, ExpenseTypeService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IIncomeService, IncomeService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IBankAccountService, BankAccountService>();
        services.AddScoped<IInvoiceExpenseAutoDeductionService, InvoiceExpenseAutoDeductionService>();
        services.AddScoped<ISupportTicketService, SupportTicketService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IQuotationService, QuotationService>();
        services.AddScoped<ISiteDocumentService, SiteDocumentService>();
        services.AddScoped<IResidentContactService, ResidentContactService>();
        services.AddScoped<IImportantPhoneService, ImportantPhoneService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPaidExpenseNotificationService, PaidExpenseNotificationService>();

        services.AddHostedService<HostedServices.MonthlyDuesHostedService>();
        services.AddHostedService<HostedServices.InvoiceExpenseAutoDeductionHostedService>();

        return services;
    }
}
