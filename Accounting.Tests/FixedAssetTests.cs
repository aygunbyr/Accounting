using Accounting.Application.Common.Exceptions;
using Accounting.Application.FixedAssets.Commands.Create;
using Accounting.Application.FixedAssets.Commands.Delete;
using Accounting.Application.FixedAssets.Commands.Update;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Accounting.Tests
{
    public class FixedAssetTests
    {
        private DbContextOptions<AppDbContext> _options;

        public FixedAssetTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
        }

        [Fact]
        public async Task Create_ShouldCalculateDepreciationRate()
        {
            var audit = new AuditSaveChangesInterceptor();
            using (var db = new AppDbContext(_options, audit))
            {
                // Seed Branch
                db.Branches.Add(new Branch { Id = 1, Name = "Main", Code = "BR-01" });
                await db.SaveChangesAsync();

                var handler = new CreateFixedAssetHandler(db);
                var cmd = new CreateFixedAssetCommand(
                    BranchId: 1,
                    Code: "FA-001",
                    Name: "Laptop",
                    PurchaseDateUtc: DateTime.UtcNow,
                    PurchasePrice: 50000m,
                    UsefulLifeYears: 5
                );

                var result = await handler.Handle(cmd, CancellationToken.None);

                var entity = await db.FixedAssets.FindAsync(result.Id);
                Assert.NotNull(entity);
                Assert.Equal(20m, entity.DepreciationRatePercent); // 100 / 5 = 20
                Assert.Equal("FA-001", entity.Code);
            }
        }

        [Fact]
        public async Task Update_ShouldRecalculateRate_WhenLifeYearsChanged()
        {
            var audit = new AuditSaveChangesInterceptor();
            using (var db = new AppDbContext(_options, audit))
            {
                db.Branches.Add(new Branch { Id = 1, Name = "Main", Code = "BR-01" });
                var asset = new FixedAsset 
                { 
                    BranchId = 1, 
                    Code = "FA-002", 
                    Name = "Table", 
                    PurchasePrice = 1000, 
                    UsefulLifeYears = 10, 
                    DepreciationRatePercent = 10,
                    RowVersion = new byte[] { 1 }
                };
                db.FixedAssets.Add(asset);
                await db.SaveChangesAsync();

                // Update
                var handler = new UpdateFixedAssetHandler(db);
                var rowVersionBase64 = Convert.ToBase64String(asset.RowVersion);
                var cmd = new UpdateFixedAssetCommand(
                    asset.Id, 
                    rowVersionBase64, 
                    "FA-002", 
                    "Office Table", 
                    DateTime.UtcNow, // Added PurchaseDate
                    1000m,           // Added PurchasePrice
                    4
                ); 

                await handler.Handle(cmd, CancellationToken.None);

                var updated = await db.FixedAssets.FindAsync(asset.Id);
                Assert.Equal(25m, updated.DepreciationRatePercent); // 100 / 4 = 25
                Assert.Equal("Office Table", updated.Name);
            }
        }

        [Fact]
        public async Task Delete_ShouldSoftDelete()
        {
            var audit = new AuditSaveChangesInterceptor();
            using (var db = new AppDbContext(_options, audit))
            {
                db.Branches.Add(new Branch { Id = 1, Name = "Main", Code = "BR-01" });
                var asset = new FixedAsset
                {
                    BranchId = 1,
                    Code = "FA-003",
                    Name = "Old Chair",
                    UsefulLifeYears = 5,
                    RowVersion = new byte[] { 1 }
                };
                db.FixedAssets.Add(asset);
                await db.SaveChangesAsync();

                var handler = new DeleteFixedAssetHandler(db);
                var rowVersion = Convert.ToBase64String(asset.RowVersion);
                var cmd = new DeleteFixedAssetCommand(asset.Id, rowVersion);

                await handler.Handle(cmd, CancellationToken.None);

                var deleted = await db.FixedAssets.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == asset.Id);
                Assert.True(deleted.IsDeleted);
                Assert.NotNull(deleted.DeletedAtUtc);
            }
        }
    }
}
