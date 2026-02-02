using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Infrastructure.Data;
using SiteYonetim.Infrastructure.Services;
using Xunit;

namespace SiteYonetim.Tests;

public class SiteServiceTests
{
    private static SiteYonetimDbContext CreateInMemoryContext(string dbName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<SiteYonetimDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SiteYonetimDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_SiteEklenir_IdAtanir()
    {
        await using var db = CreateInMemoryContext(nameof(CreateAsync_SiteEklenir_IdAtanir));
        var service = new SiteService(db);

        var site = new Site
        {
            Name = "Yeni Site",
            Address = "Adres",
            City = "Ankara",
            IsDeleted = false
        };

        var result = await service.CreateAsync(site);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Yeni Site", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_KayitVarsaDoner()
    {
        await using var db = CreateInMemoryContext(nameof(GetByIdAsync_KayitVarsaDoner));
        var service = new SiteService(db);
        var site = new Site { Name = "Test", IsDeleted = false };
        await service.CreateAsync(site);

        var found = await service.GetByIdAsync(site.Id);

        Assert.NotNull(found);
        Assert.Equal(site.Id, found.Id);
        Assert.Equal("Test", found.Name);
    }

    [Fact]
    public async Task GetByIdAsync_SilinmisSiteIcinNullDoner()
    {
        await using var db = CreateInMemoryContext(nameof(GetByIdAsync_SilinmisSiteIcinNullDoner));
        var service = new SiteService(db);
        var site = new Site { Name = "Test", IsDeleted = false };
        await service.CreateAsync(site);
        await service.DeleteAsync(site.Id);

        var found = await service.GetByIdAsync(site.Id);

        Assert.Null(found);
    }
}
