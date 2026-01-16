using Gurung.BulkOperations.SqlServer.Tests.Context;
using Gurung.BulkOperations.SqlServer.Tests.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using Testcontainers.MsSql;

namespace Gurung.BulkOperations.SqlServer.Tests.SqlServer
{
    public class SQLServerBulkInsertIntegrationTests : IAsyncLifetime
    {
        private readonly MsSqlContainer _sqlContainer;
        private AppDbContext _context;

        public SQLServerBulkInsertIntegrationTests()
        {
            _sqlContainer = new MsSqlBuilder()
                .WithPassword("yourStrong(!)Password")
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();
        }

        public async Task DisposeAsync()
        {
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

        [Fact]
        public async Task BulkInsert_ShouldInsertRecords_Quickly_AndAccurately()
        {
            // Arrange
            var records = Enumerable.Range(1, 5000).Select(i => new UserEntity
            {
                Name = $"User_{i}",
                Email = $"user{i}@test.com",
                CreatedAt = DateTime.UtcNow
            }).ToList();

            // Act
            var stopwatch = Stopwatch.StartNew();
            await _context.Users.BulkInsertAsync(records);

            // await _bulkOperations.BulkInsertAsync(records);
            stopwatch.Stop();

            // Assert
            var elapsed = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Inserted {records.Count} records in {elapsed} ms");

            // Assert.True(elapsed < 2000, $"Bulk insert took too long: {elapsed} ms");

            var count = await _context.Users.CountAsync();
            Assert.Equal(5000, count);
        }

        [Fact]
        public async Task BulkUpdate_ShouldUpdateRecords_Quickly_AndAccurately()
        {
            // Arrange: Insert initial records
            var records = Enumerable.Range(1, 5000).Select(i => new UserEntity
            {
                Name = $"User_{i}",
                Email = $"user{i}@test.com",
                CreatedAt = DateTime.UtcNow
            }).ToList();
            await _context.BulkInsertAsync(records);

            // Modify them for update
            foreach (var user in records)
            {
                user.Email = $"updated_{user.Email}";
            }

            // Act
            var stopwatch = Stopwatch.StartNew();
            await _context.BulkUpDateAsync(records);
            stopwatch.Stop();

            // Assert
            var elapsed = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Bulk update completed in {elapsed} ms");

            Assert.True(elapsed < 2500, $"Bulk update took too long: {elapsed} ms");

            var updatedCount = await _context.Users
                .CountAsync(u => u.Email.StartsWith("updated_"));
            Assert.Equal(5000, updatedCount);
        }
    }
}