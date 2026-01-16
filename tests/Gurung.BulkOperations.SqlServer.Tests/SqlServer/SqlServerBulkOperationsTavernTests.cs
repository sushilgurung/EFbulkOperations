using Gurung.BulkOperations.Core.Entity;
using Gurung.BulkOperations.SqlServer.Tests.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace Gurung.BulkOperations.SqlServer.Tests.SqlServer
{
    /// <summary>
    /// Integration tests for SQL Server bulk operations with Tavern entity.
    /// Tests that [Column] attributes are properly respected for all operations:
    /// - BulkInsert, BulkUpdate, BulkInsertOrUpdate
    /// - With and without IDENTITY_INSERT (KeepIdentity)
    /// </summary>
    public class SqlServerBulkOperationsTavernTests : IAsyncLifetime
    {
        private readonly MsSqlContainer _sqlContainer;
        private AppDbContext _context;

        public SqlServerBulkOperationsTavernTests()
        {
            _sqlContainer = new MsSqlBuilder()
                .WithPassword("yourStrong(!)Password")
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();
        }

        public async Task DisposeAsync()
        {
            if (_context != null)
            {
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE taverns;");
                await _context.DisposeAsync();
            }
            await _sqlContainer.StopAsync();
            await _sqlContainer.DisposeAsync();
        }

        public async Task InitializeAsync()
        {
            await _sqlContainer.StartAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_sqlContainer.GetConnectionString())
                .Options;

            _context = new AppDbContext(options);
            await _context.Database.EnsureCreatedAsync();
        }

        #region BulkInsert Tests

        [Fact]
        public async Task BulkInsert_WithColumnAttributes_ShouldRespectDatabaseColumnNames()
        {
            // Arrange - Create taverns with Column attribute mappings
            var taverns = new List<Tavern>
            {
                new Tavern
                {
                    UserId = "user_001",
                    UserName = "johndoe",
                    Password = "hashedpass123",
                    Email = "john@tavern.com",
                    TavernName = "The Golden Dragon",
                    StreetAddress = "123 Main St",
                    City = "New York",
                    StateId = 1,
                    ZipCode = "10001",
                    CountryId = 1,
                    LocationId = 1,
                    Phone = "555-0100",
                    FirstName = "John",
                    LastName = "Doe",
                    MaxNumberOfTeams = 20,
                    IsActive = true,
                    Activity = DateTime.UtcNow,
                    AutoGameMode = 1,
                    TvVersion = false,
                    ChkOk = true,
                    OwnerId = 1
                },
                new Tavern
                {
                    UserId = "user_002",
                    UserName = "janedoe",
                    Password = "hashedpass456",
                    Email = "jane@tavern.com",
                    TavernName = "The Silver Fox",
                    StreetAddress = "456 Oak Ave",
                    City = "Boston",
                    StateId = 2,
                    ZipCode = "02101",
                    CountryId = 1,
                    LocationId = 2,
                    Phone = "555-0200",
                    FirstName = "Jane",
                    LastName = "Doe",
                    MaxNumberOfTeams = 15,
                    IsActive = true,
                    Activity = DateTime.UtcNow,
                    AutoGameMode = 1,
                    TvVersion = false,
                    ChkOk = true,
                    OwnerId = 2
                }
            };

            var stopwatch = Stopwatch.StartNew();

            // Act - Insert with auto-generated IDs
            await _context.Taverns.BulkInsertAsync(taverns, new BulkConfig { KeepIdentity = false });
            stopwatch.Stop();

            Console.WriteLine($"BulkInsert with [Column] attributes completed in {stopwatch.ElapsedMilliseconds} ms");

            // Assert
            var count = await _context.Taverns.CountAsync();
            Assert.Equal(2, count);

            var goldenDragon = await _context.Taverns.FirstOrDefaultAsync(t => t.TavernName == "The Golden Dragon");
            Assert.NotNull(goldenDragon);
            Assert.True(goldenDragon.Id > 0);
            Assert.Equal("john@tavern.com", goldenDragon.Email);
            Assert.Equal("123 Main St", goldenDragon.StreetAddress);
            Assert.Equal("555-0100", goldenDragon.Phone);
            Assert.Equal("user_001", goldenDragon.UserId);
        }

        [Fact]
        public async Task BulkInsert_WithKeepIdentityTrue_ShouldPreserveExplicitIds()
        {
            // Arrange
            var taverns = new List<Tavern>
            {
                new Tavern
                {
                    Id = 1000,
                    UserId = "user_1000",
                    UserName = "tavern1000",
                    Password = "pass1000",
                    Email = "tavern1000@test.com",
                    TavernName = "Tavern 1000",
                    City = "Chicago",
                    StateId = 3,
                    CountryId = 1,
                    LocationId = 1000,
                    MaxNumberOfTeams = 10,
                    IsActive = true,
                    Activity = DateTime.UtcNow,
                    AutoGameMode = 1,
                    TvVersion = false,
                    ChkOk = true,
                    OwnerId = 1000
                }
            };

            // Act
            await _context.Taverns.BulkInsertAsync(taverns, new BulkConfig { KeepIdentity = true });

            // Assert
            var tavern = await _context.Taverns.FindAsync(1000L);
            Assert.NotNull(tavern);
            Assert.Equal(1000, tavern.Id);
            Assert.Equal("Tavern 1000", tavern.TavernName);
        }

        [Fact]
        public async Task BulkInsert_LargeDatasetWithColumnAttributes_ShouldBePerformant()
        {
            // Arrange
            var taverns = Enumerable.Range(1, 1000).Select(i => new Tavern
            {
                UserId = $"user_{i}",
                UserName = $"username{i}",
                Password = $"pass{i}",
                Email = $"tavern{i}@test.com",
                TavernName = $"Tavern_{i}",
                StreetAddress = $"{i} Main St",
                City = "TestCity",
                StateId = 1,
                ZipCode = $"{10000 + i}",
                CountryId = 1,
                LocationId = i,
                Phone = $"555-{i:D4}",
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                MaxNumberOfTeams = 20,
                IsActive = true,
                Activity = DateTime.UtcNow,
                AutoGameMode = 1,
                TvVersion = false,
                ChkOk = true,
                OwnerId = i
            }).ToList();

            var stopwatch = Stopwatch.StartNew();

            // Act
            await _context.Taverns.BulkInsertAsync(taverns);
            stopwatch.Stop();

            Console.WriteLine($"Inserted 1,000 taverns with [Column] attributes in {stopwatch.ElapsedMilliseconds} ms");

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Insert took {stopwatch.ElapsedMilliseconds} ms");

            var count = await _context.Taverns.CountAsync();
            Assert.Equal(1000, count);

            // Verify random sample
            var sample = await _context.Taverns.FirstOrDefaultAsync(t => t.TavernName == "Tavern_500");
            Assert.NotNull(sample);
            Assert.Equal("user_500", sample.UserId);
        }

        #endregion

        #region BulkUpdate Tests

        [Fact]
        public async Task BulkUpdate_WithColumnAttributes_ShouldUpdateCorrectly()
        {
            // Arrange - Insert initial data
            var taverns = new List<Tavern>
            {
                new Tavern
                {
                    UserId = "user_001",
                    UserName = "original",
                    Password = "originalpass",
                    Email = "original@test.com",
                    TavernName = "Original Tavern",
                    City = "OldCity",
                    StateId = 1,
                    CountryId = 1,
                    LocationId = 1,
                    MaxNumberOfTeams = 10,
                    IsActive = true,
                    Activity = DateTime.UtcNow,
                    AutoGameMode = 1,
                    TvVersion = false,
                    ChkOk = true,
                    OwnerId = 1
                }
            };

            await _context.Taverns.BulkInsertAsync(taverns);

            // Get inserted tavern with ID
            var insertedTavern = await _context.Taverns.FirstAsync();

            // Modify for update
            insertedTavern.Email = "updated@test.com";
            insertedTavern.TavernName = "Updated Tavern";
            insertedTavern.City = "NewCity";
            insertedTavern.MaxNumberOfTeams = 25;

            var stopwatch = Stopwatch.StartNew();

            // Act - Update using column mappings
            await _context.Taverns.BulkUpDateAsync(new List<Tavern> { insertedTavern });
            stopwatch.Stop();

            Console.WriteLine($"BulkUpdate with [Column] attributes completed in {stopwatch.ElapsedMilliseconds} ms");

            // Assert
            var updated = await _context.Taverns.FindAsync(insertedTavern.Id);
            Assert.NotNull(updated);
            Assert.Equal("updated@test.com", updated.Email);
            Assert.Equal("Updated Tavern", updated.TavernName);
            Assert.Equal("NewCity", updated.City);
            Assert.Equal(25, updated.MaxNumberOfTeams);
        }

        [Fact]
        public async Task BulkUpdate_LargeDataset_ShouldUpdateAllRecords()
        {
            // Arrange - Insert 500 taverns
            var taverns = Enumerable.Range(1, 500).Select(i => new Tavern
            {
                UserId = $"user_{i}",
                UserName = $"user{i}",
                Password = $"pass{i}",
                Email = $"tavern{i}@test.com",
                TavernName = $"Tavern_{i}",
                City = "OldCity",
                StateId = 1,
                CountryId = 1,
                LocationId = i,
                MaxNumberOfTeams = 10,
                IsActive = true,
                Activity = DateTime.UtcNow,
                AutoGameMode = 1,
                TvVersion = false,
                ChkOk = true,
                OwnerId = i
            }).ToList();

            await _context.Taverns.BulkInsertAsync(taverns);

            // Get all and modify
            var allTaverns = await _context.Taverns.ToListAsync();
            foreach (var tavern in allTaverns)
            {
                tavern.City = "UpdatedCity";
                tavern.Email = $"updated_{tavern.Email}";
                tavern.MaxNumberOfTeams = 99;
            }

            var stopwatch = Stopwatch.StartNew();

            // Act
            await _context.Taverns.BulkUpDateAsync(allTaverns);
            stopwatch.Stop();

            Console.WriteLine($"Updated 500 taverns in {stopwatch.ElapsedMilliseconds} ms");

            // Assert
            var updatedCount = await _context.Taverns.CountAsync(t => t.City == "UpdatedCity");
            Assert.Equal(500, updatedCount);

            var emailCount = await _context.Taverns.CountAsync(t => t.Email.StartsWith("updated_"));
            Assert.Equal(500, emailCount);
        }

        #endregion

        #region BulkInsertOrUpdate Tests

        [Fact]
        public async Task BulkInsertOrUpdate_MixedOperation_ShouldHandleInsertAndUpdate()
        {
            // Arrange - Phase 1: Insert initial taverns
            var initialTaverns = new List<Tavern>
            {
                new Tavern
                {
                    UserId = "user_001",
                    UserName = "existing1",
                    Password = "pass1",
                    Email = "existing1@test.com",
                    TavernName = "Existing Tavern 1",
                    City = "City1",
                    StateId = 1,
                    CountryId = 1,
                    LocationId = 1,
                    MaxNumberOfTeams = 10,
                    IsActive = true,
                    Activity = DateTime.UtcNow,
                    AutoGameMode = 1,
                    TvVersion = false,
                    ChkOk = true,
                    OwnerId = 1
                },
                new Tavern
                {
                    UserId = "user_002",
                    UserName = "existing2",
                    Password = "pass2",
                    Email = "existing2@test.com",
                    TavernName = "Existing Tavern 2",
                    City = "City2",
                    StateId = 2,
                    CountryId = 1,
                    LocationId = 2,
                    MaxNumberOfTeams = 15,
                    IsActive = true,
                    Activity = DateTime.UtcNow,
                    AutoGameMode = 1,
                    TvVersion = false,
                    ChkOk = true,
                    OwnerId = 2
                }
            };

            await _context.Taverns.BulkInsertAsync(initialTaverns);
            var existingTaverns = await _context.Taverns.ToListAsync();

            // Create mixed list: update existing + insert new
            var mixedList = new List<Tavern>();

            // Update first existing tavern
            existingTaverns[0].Email = "updated_existing1@test.com";
            existingTaverns[0].TavernName = "Updated Existing 1";
            mixedList.Add(existingTaverns[0]);

            // Add new taverns
            mixedList.Add(new Tavern
            {
                UserId = "user_003",
                UserName = "new1",
                Password = "newpass1",
                Email = "new1@test.com",
                TavernName = "New Tavern 1",
                City = "NewCity",
                StateId = 3,
                CountryId = 1,
                LocationId = 3,
                MaxNumberOfTeams = 20,
                IsActive = true,
                Activity = DateTime.UtcNow,
                AutoGameMode = 1,
                TvVersion = false,
                ChkOk = true,
                OwnerId = 3
            });

            var stopwatch = Stopwatch.StartNew();

            // Act - BulkInsertOrUpdate
            await _context.Taverns.BulkInsertOrUpDateAsync(mixedList);
            stopwatch.Stop();

            Console.WriteLine($"BulkInsertOrUpdate (1 update + 1 insert) completed in {stopwatch.ElapsedMilliseconds} ms");

            // Assert
            var totalCount = await _context.Taverns.CountAsync();
            Assert.Equal(3, totalCount); // 2 original + 1 new

            // Verify update
            var updated = await _context.Taverns.FirstOrDefaultAsync(t => t.TavernName == "Updated Existing 1");
            Assert.NotNull(updated);
            Assert.Equal("updated_existing1@test.com", updated.Email);

            // Verify insert
            var newTavern = await _context.Taverns.FirstOrDefaultAsync(t => t.TavernName == "New Tavern 1");
            Assert.NotNull(newTavern);
            Assert.Equal("new1@test.com", newTavern.Email);
        }

        [Fact]
        public async Task BulkInsertOrUpdate_LargeScaleMixedOperation_ShouldHandleCorrectly()
        {
            // Arrange - Insert 300 taverns
            var initialTaverns = Enumerable.Range(1, 300).Select(i => new Tavern
            {
                UserId = $"user_{i}",
                UserName = $"user{i}",
                Password = $"pass{i}",
                Email = $"tavern{i}@test.com",
                TavernName = $"Tavern_{i}",
                City = "InitialCity",
                StateId = 1,
                CountryId = 1,
                LocationId = i,
                MaxNumberOfTeams = 10,
                IsActive = true,
                Activity = DateTime.UtcNow,
                AutoGameMode = 1,
                TvVersion = false,
                ChkOk = true,
                OwnerId = i
            }).ToList();

            await _context.Taverns.BulkInsertAsync(initialTaverns);

            // Get existing taverns and modify first 150
            var existingTaverns = await _context.Taverns.Take(150).ToListAsync();
            foreach (var tavern in existingTaverns)
            {
                tavern.City = "UpdatedCity";
                tavern.Email = $"updated_{tavern.Email}";
            }

            // Create 100 new taverns
            var newTaverns = Enumerable.Range(301, 100).Select(i => new Tavern
            {
                UserId = $"user_{i}",
                UserName = $"newuser{i}",
                Password = $"newpass{i}",
                Email = $"newtavern{i}@test.com",
                TavernName = $"NewTavern_{i}",
                City = "NewCity",
                StateId = 1,
                CountryId = 1,
                LocationId = i,
                MaxNumberOfTeams = 20,
                IsActive = true,
                Activity = DateTime.UtcNow,
                AutoGameMode = 1,
                TvVersion = false,
                ChkOk = true,
                OwnerId = i
            }).ToList();

            var mixedList = existingTaverns.Concat(newTaverns).ToList();

            var stopwatch = Stopwatch.StartNew();

            // Act
            await _context.Taverns.BulkInsertOrUpDateAsync(mixedList);
            stopwatch.Stop();

            Console.WriteLine($"BulkInsertOrUpdate (150 updates + 100 inserts) completed in {stopwatch.ElapsedMilliseconds} ms");

            // Assert
            var totalCount = await _context.Taverns.CountAsync();
            Assert.Equal(400, totalCount); // 300 original + 100 new

            var updatedCount = await _context.Taverns.CountAsync(t => t.City == "UpdatedCity");
            Assert.Equal(150, updatedCount);

            var newCount = await _context.Taverns.CountAsync(t => t.TavernName.StartsWith("NewTavern_"));
            Assert.Equal(100, newCount);
        }

        #endregion

        #region Column Attribute Validation Tests

        [Fact]
        public async Task BulkOperations_ShouldHandleAllColumnAttributeTypes()
        {
            // Arrange - Tavern with all types of column mappings
            var tavern = new Tavern
            {
                UserId = "test_user",
                UserName = "testuser",
                Password = "testpass",
                Email = "test@test.com",
                TavernName = "Test Tavern",
                OwnerName = "Test Owner",
                Gender = "Other",
                StreetAddress = "123 Test St",
                City = "TestCity",
                StateId = 1,
                ZipCode = "12345",
                CountryId = 1,
                LocationId = 1,
                County = "Test County",
                Phone = "555-1234",
                Fax = "555-5678",
                WebsiteUrl = "https://test.com",
                FirstName = "Test",
                LastName = "User",
                CellPhone = "555-9999",
                BigBrainFirstName = "Brain",
                BigBrainLastName = "Master",
                BigBrainEmail = "brain@test.com",
                BigBrainCell = "555-7777",
                MaxTable = 5,
                FirstNight = "Friday",
                FirstNightTime1 = "7:00 PM",
                FirstNightTime2 = "9:00 PM",
                SecondNight = "Saturday",
                SecondNightTime1 = "7:00 PM",
                SecondNightTime2 = "9:00 PM",
                CheckInTime = "6:30 PM",
                RegistrationCloses = "6:45 PM",
                RegistrationOpens = "6:00 PM",
                MaxNumberOfTeams = 30,
                IsActive = true,
                TimeZone = "EST",
                Activity = DateTime.UtcNow,
                NumberOfTimeUpdate = 1,
                OwnerId = 1,
                Notes = "Test notes",
                AverageTeams = 25,
                BonusQuestions = 3,
                BirthDate = DateTime.Now.AddYears(-30),
                PlayersPerTable = 6,
                BigBrainFirstName2 = "Brain2",
                BigBrainEmail2 = "brain2@test.com",
                BigBrainLastName2 = "Master2",
                BigBrainCell2 = "555-6666",
                WeeklyFirstPrize = "$100",
                WeeklySecondPrize = "$50",
                WeeklyThirdPrize = "$25",
                WeeklySpecials = "Happy Hour",
                TvVersion = true,
                ShippingAddress = "456 Ship St",
                ShippingCity = "ShipCity",
                ShippingStateId = 2,
                ShippingZip = "67890",
                AutoGameMode = 2,
                FoodSpecial = "Pizza",
                DrinkSpecial = "Beer",
                ChkOk = true
            };

            // Act - Insert
            await _context.Taverns.BulkInsertAsync(new List<Tavern> { tavern });

            // Assert - Verify all fields were inserted correctly
            var inserted = await _context.Taverns.FirstAsync();
            Assert.Equal("Test Tavern", inserted.TavernName);
            Assert.Equal("test_user", inserted.UserId);
            Assert.Equal("Test County", inserted.County);
            Assert.Equal("Friday", inserted.FirstNight);
            Assert.Equal("$100", inserted.WeeklyFirstPrize);
            Assert.True(inserted.TvVersion);
            Assert.Equal("Pizza", inserted.FoodSpecial);
            Assert.Equal(2, inserted.AutoGameMode);

            // Act - Update
            inserted.TavernName = "Updated Test Tavern";
            inserted.WeeklyFirstPrize = "$200";
            inserted.AutoGameMode = 3;
            await _context.Taverns.BulkUpDateAsync(new List<Tavern> { inserted });

            // Assert - Verify update
            var updated = await _context.Taverns.FindAsync(inserted.Id);
            Assert.Equal("Updated Test Tavern", updated.TavernName);
            Assert.Equal("$200", updated.WeeklyFirstPrize);
            Assert.Equal(3, updated.AutoGameMode);
        }

        [Fact]
        public async Task BulkOperations_WithNullableColumns_ShouldHandleNullValues()
        {
            // Arrange - Tavern with many null values
            var tavern = new Tavern
            {
                UserId = "min_user",
                UserName = "minuser",
                Password = "minpass",
                Email = "min@test.com",
                TavernName = null, // Nullable
                OwnerName = null,
                Gender = null,
                City = "MinCity",
                StateId = 1,
                CountryId = 1,
                LocationId = 1,
                MaxNumberOfTeams = 10,
                IsActive = true,
                Activity = DateTime.UtcNow,
                AutoGameMode = 1,
                TvVersion = false,
                ChkOk = false,
                OwnerId = 1
            };

            // Act
            await _context.Taverns.BulkInsertAsync(new List<Tavern> { tavern });

            // Assert
            var inserted = await _context.Taverns.FirstAsync();
            Assert.Null(inserted.TavernName);
            Assert.Null(inserted.OwnerName);
            Assert.Null(inserted.Gender);
            Assert.Equal("MinCity", inserted.City);
        }

        #endregion
    }
}
