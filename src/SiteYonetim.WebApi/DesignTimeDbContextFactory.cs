using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.WebApi;

/// <summary>
/// EF Core migrations için design-time factory.
/// Komut: dotnet ef migrations add InitialCreate --project ../SiteYonetim.Infrastructure
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SiteYonetimDbContext>
{
    public SiteYonetimDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
        var optionsBuilder = new DbContextOptionsBuilder<SiteYonetimDbContext>();
        optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly(typeof(SiteYonetimDbContext).Assembly.FullName));
        return new SiteYonetimDbContext(optionsBuilder.Options);
    }
}
