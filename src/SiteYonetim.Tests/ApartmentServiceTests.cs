using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Infrastructure.Data;
using SiteYonetim.Infrastructure.Services;
using Xunit;

namespace SiteYonetim.Tests;

public class ApartmentServiceTests
{
    private static SiteYonetimDbContext CreateInMemoryContext(string dbName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<SiteYonetimDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SiteYonetimDbContext(options);
    }

    private static async Task<Guid> SeedSiteAsync(SiteYonetimDbContext db)
    {
        var site = new Site
        {
            Name = "Test Site",
            Address = "Test Adres",
            City = "İstanbul",
            IsDeleted = false
        };
        db.Sites.Add(site);
        await db.SaveChangesAsync();
        return site.Id;
    }

    [Fact]
    public async Task CreateAsync_EklenenDaire_IdVeSiteIdAtanir()
    {
        await using var db = CreateInMemoryContext(nameof(CreateAsync_EklenenDaire_IdVeSiteIdAtanir));
        var siteId = await SeedSiteAsync(db);
        var service = new ApartmentService(db);

        var apartment = new Apartment
        {
            SiteId = siteId,
            ApartmentNumber = "1",
            BlockOrBuildingName = "A Blok",
            ShareRate = 1,
            IsDeleted = false
        };

        var result = await service.CreateAsync(apartment);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(siteId, result.SiteId);
        Assert.Equal("1", result.ApartmentNumber);
        Assert.True(result.CreatedAt <= DateTime.UtcNow && result.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task GetBySiteIdAsync_SadeceIlgiliSiteninDaireleriniDoner()
    {
        await using var db = CreateInMemoryContext(nameof(GetBySiteIdAsync_SadeceIlgiliSiteninDaireleriniDoner));
        var siteId = await SeedSiteAsync(db);
        var service = new ApartmentService(db);

        await service.CreateAsync(new Apartment { SiteId = siteId, ApartmentNumber = "1", BlockOrBuildingName = "A", IsDeleted = false });
        await service.CreateAsync(new Apartment { SiteId = siteId, ApartmentNumber = "2", BlockOrBuildingName = "A", IsDeleted = false });

        var list = await service.GetBySiteIdAsync(siteId);
        Assert.Equal(2, list.Count);
        Assert.All(list, a => Assert.Equal(siteId, a.SiteId));
    }

    [Fact]
    public async Task GetBySiteIdAsync_SilinmisDaireleriDonmez()
    {
        await using var db = CreateInMemoryContext(nameof(GetBySiteIdAsync_SilinmisDaireleriDonmez));
        var siteId = await SeedSiteAsync(db);
        var service = new ApartmentService(db);

        var apt = new Apartment { SiteId = siteId, ApartmentNumber = "1", BlockOrBuildingName = "A", IsDeleted = false };
        await service.CreateAsync(apt);
        await service.DeleteAsync(apt.Id);

        var list = await service.GetBySiteIdAsync(siteId);
        Assert.Empty(list);
    }

    [Fact]
    public async Task UpdateAsync_AlanlariGunceller()
    {
        await using var db = CreateInMemoryContext(nameof(UpdateAsync_AlanlariGunceller));
        var siteId = await SeedSiteAsync(db);
        var service = new ApartmentService(db);

        var apt = await service.CreateAsync(new Apartment
        {
            SiteId = siteId,
            ApartmentNumber = "1",
            BlockOrBuildingName = "A Blok",
            OwnerName = "Eski",
            IsDeleted = false
        });

        apt.OwnerName = "Yeni Malik";
        apt.ApartmentNumber = "1-A";
        await service.UpdateAsync(apt);

        var updated = await service.GetByIdAsync(apt.Id);
        Assert.NotNull(updated);
        Assert.Equal("Yeni Malik", updated.OwnerName);
        Assert.Equal("1-A", updated.ApartmentNumber);
    }

    [Fact]
    public async Task GetByIdAsync_SilinmisDaireIcinNullDoner()
    {
        await using var db = CreateInMemoryContext(nameof(GetByIdAsync_SilinmisDaireIcinNullDoner));
        var siteId = await SeedSiteAsync(db);
        var service = new ApartmentService(db);

        var apt = await service.CreateAsync(new Apartment { SiteId = siteId, ApartmentNumber = "1", BlockOrBuildingName = "A", IsDeleted = false });
        await service.DeleteAsync(apt.Id);

        var found = await service.GetByIdAsync(apt.Id);
        Assert.Null(found);
    }
}
