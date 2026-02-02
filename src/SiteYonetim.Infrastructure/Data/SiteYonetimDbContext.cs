using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Infrastructure.Data;

public class SiteYonetimDbContext : DbContext
{
    public SiteYonetimDbContext(DbContextOptions<SiteYonetimDbContext> options) : base(options) { }

    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Apartment> Apartments => Set<Apartment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSite> UserSites => Set<UserSite>();
    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<ExpenseType> ExpenseTypes => Set<ExpenseType>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseShare> ExpenseShares => Set<ExpenseShare>();
    public DbSet<Meter> Meters => Set<Meter>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<RecurringCharge> RecurringCharges => Set<RecurringCharge>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportTicketMessage> SupportTicketMessages => Set<SupportTicketMessage>();
    public DbSet<SupportTicketAttachment> SupportTicketAttachments => Set<SupportTicketAttachment>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();
    public DbSet<SurveyOption> SurveyOptions => Set<SurveyOption>();
    public DbSet<SurveyVote> SurveyVotes => Set<SurveyVote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Site
        modelBuilder.Entity<Site>(e =>
        {
            e.HasIndex(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.District).HasMaxLength(100);
        });

        // Apartment
        modelBuilder.Entity<Apartment>(e =>
        {
            e.HasIndex(x => new { x.SiteId, x.ApartmentNumber }).IsUnique();
            e.HasIndex(x => x.SiteId);
            e.Property(x => x.ApartmentNumber).HasMaxLength(50);
            e.Property(x => x.BlockOrBuildingName).HasMaxLength(100);
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.FullName).HasMaxLength(200);
        });

        // ExpenseShare - unique (ExpenseId, ApartmentId)
        modelBuilder.Entity<ExpenseShare>(e =>
        {
            e.HasIndex(x => new { x.ExpenseId, x.ApartmentId }).IsUnique();
            e.HasIndex(x => x.ApartmentId);
            e.HasIndex(x => x.Status);
        });

        // Payment
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasIndex(x => new { x.SiteId, x.PaymentDate });
            e.HasIndex(x => x.ApartmentId);
        });

        // MeterReading
        modelBuilder.Entity<MeterReading>(e =>
        {
            e.HasIndex(x => new { x.MeterId, x.ReadingDate });
        });

        // Receipt - Payment ile 1-1 ilişki: Receipt bağımlı (PaymentId FK)
        modelBuilder.Entity<Receipt>(e =>
        {
            e.HasIndex(x => new { x.SiteId, x.ReceiptNumber }).IsUnique();
            e.HasOne(x => x.Payment)
                .WithOne(x => x.Receipt)
                .HasForeignKey<Receipt>(x => x.PaymentId)
                .IsRequired();
        });

        // Income - Site, Apartment; 1-1 ilişki Payment ile (Payment bağımlı, IncomeId FK)
        modelBuilder.Entity<Income>(e =>
        {
            e.HasIndex(x => new { x.SiteId, x.ApartmentId, x.Year, x.Month });
            e.HasIndex(x => x.SiteId);
            e.HasIndex(x => x.ApartmentId);
            e.HasOne(x => x.Payment)
                .WithOne(x => x.Income)
                .HasForeignKey<Payment>(x => x.IncomeId)
                .IsRequired(false);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.Id = Guid.NewGuid();
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
