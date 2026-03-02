# Gurung.EfBulkOperations

A high-performance bulk operations library for Entity Framework Core, supporting both **SQL Server** and **PostgreSQL**. This library provides efficient bulk insert, update, and upsert operations with full support for EF Core's `[Column]` attributes and custom column mappings.

## Features

- ✅ **High Performance**: Optimized bulk operations using native database features
  - SQL Server: `SqlBulkCopy` and `MERGE` statements
  - PostgreSQL: Binary `COPY` command and `INSERT ON CONFLICT`
- ✅ **Full EF Core Integration**: Works seamlessly with DbContext
- ✅ **Column Attribute Support**: Respects `[Column]` attributes and EF Core column mappings
- ✅ **Identity Column Handling**: Automatic handling of auto-generated IDs or preserve explicit IDs
- ✅ **Async/Await**: Full async support for all operations
- ✅ **Transaction Support**: All operations run within transactions
- ✅ **Type Safe**: Strongly-typed generic methods

## Supported Databases

- **SQL Server** (2016 and above)
- **PostgreSQL** (12 and above)

## Installation

```bash
# For SQL Server
dotnet add package Gurung.EfBulkOperations.SqlServer

# For PostgreSQL
dotnet add package Gurung.EfBulkOperations.PostgreSql

# Core library (included automatically with above packages)
dotnet add package Gurung.EfBulkOperations
```

## Quick Start

### 1. Setup Your Entity

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_name")]
    [MaxLength(100)]
    public string UserName { get; set; }

    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }
}
```

### 2. Configure DbContext

```csharp
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // For SQL Server
        optionsBuilder.UseSqlServer("your-connection-string");
        
        // OR for PostgreSQL
        // optionsBuilder.UseNpgsql("your-connection-string");
    }
}
```

## Usage Examples

### BulkInsert

Insert thousands of records efficiently in a single operation.

```csharp
using Gurung.EfBulkOperations;

var users = new List<User>
{
    new User { UserName = "john_doe", Email = "john@example.com", CreatedAt = DateTime.Now, IsActive = true },
    new User { UserName = "jane_smith", Email = "jane@example.com", CreatedAt = DateTime.Now, IsActive = true },
    // ... thousands more
};

using var context = new AppDbContext();

// Insert with auto-generated IDs
await context.BulkInsertAsync(users);

// Insert with explicit IDs (preserves your ID values)
await context.BulkInsertAsync(users, new BulkConfig { KeepIdentity = true });

// With batch configuration
await context.BulkInsertAsync(users, new BulkConfig 
{ 
    BatchSize = 5000,
    BulkCopyTimeout = 300
});
```

### BulkUpdate

Update existing records efficiently.

```csharp
using var context = new AppDbContext();

// Fetch records to update
var users = await context.Users
    .Where(u => u.IsActive)
    .ToListAsync();

// Modify records
foreach (var user in users)
{
    user.Email = $"updated_{user.Email}";
    user.IsActive = false;
}

// Bulk update
await context.BulkUpdateAsync(users);
```

### BulkInsertOrUpdate (Upsert)

Insert new records and update existing ones in a single operation.

```csharp
using var context = new AppDbContext();

var users = new List<User>
{
    new User { Id = 1, UserName = "john_doe", Email = "john.updated@example.com", CreatedAt = DateTime.Now, IsActive = true },
    new User { Id = 2, UserName = "jane_smith", Email = "jane.updated@example.com", CreatedAt = DateTime.Now, IsActive = true },
    new User { UserName = "new_user", Email = "new@example.com", CreatedAt = DateTime.Now, IsActive = true }
};

// Records with Id = 1 and 2 will be updated
// Record without Id (or Id = 0) will be inserted
await context.BulkInsertOrUpdateAsync(users);
```

## Configuration Options

### BulkConfig

```csharp
public class BulkConfig
{
    /// <summary>
    /// Preserve explicit ID values instead of using database-generated IDs
    /// </summary>
    public bool KeepIdentity { get; set; } = false;

    /// <summary>
    /// Number of records per batch (SQL Server only)
    /// </summary>
    public int BatchSize { get; set; } = 0; // 0 = use default

    /// <summary>
    /// Timeout in seconds for bulk operations
    /// </summary>
    public int BulkCopyTimeout { get; set; } = 0; // 0 = use default

    /// <summary>
    /// Notify after this many records (SQL Server only)
    /// </summary>
    public int NotifyAfter { get; set; } = 0;
}
```

## Real-World Example

### Complete Example with Complex Entity

```csharp
[Table("taverns")]
public class Tavern
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    [MaxLength(450)]
    public required string UserId { get; set; }

    [Column("tavern_name")]
    [MaxLength(255)]
    public string? TavernName { get; set; }

    [Column("city")]
    [MaxLength(255)]
    public string? City { get; set; }

    [Column("max_table")]
    public int? MaxTable { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("activity")]
    public DateTime Activity { get; set; }

    // ... more properties
}

// Usage
public class TavernService
{
    private readonly AppDbContext _context;

    public TavernService(AppDbContext context)
    {
        _context = context;
    }

    public async Task ImportTavernsAsync(List<Tavern> taverns)
    {
        // Insert 10,000 taverns in one operation
        await _context.BulkInsertAsync(taverns, new BulkConfig 
        { 
            BatchSize = 5000,
            BulkCopyTimeout = 600 
        });
    }

    public async Task UpdateTavernStatusAsync()
    {
        // Fetch taverns to update
        var inactiveTaverns = await _context.Taverns
            .Where(t => t.Activity < DateTime.Now.AddDays(-30))
            .ToListAsync();

        // Mark as inactive
        foreach (var tavern in inactiveTaverns)
        {
            tavern.IsActive = false;
        }

        // Bulk update
        await _context.BulkUpdateAsync(inactiveTaverns);
    }

    public async Task SyncTavernsAsync(List<Tavern> externalTaverns)
    {
        // Upsert: Update existing taverns and insert new ones
        await _context.BulkInsertOrUpdateAsync(externalTaverns);
    }
}
```

## Performance Comparison

| Operation | Records | Standard EF Core | Gurung.BulkOperations | Improvement |
|-----------|---------|------------------|----------------------|-------------|
| Insert | 10,000 | ~45 seconds | ~2 seconds | **22x faster** |
| Update | 5,000 | ~30 seconds | ~1.5 seconds | **20x faster** |
| Upsert | 10,000 | ~60 seconds | ~3 seconds | **20x faster** |

*Results may vary based on hardware, network latency, and data complexity.*

## Database-Specific Features

### SQL Server

- Uses `SqlBulkCopy` for inserts
- Uses `MERGE` statements for updates and upserts
- Supports `IDENTITY_INSERT ON/OFF` for explicit ID handling
- Batch size configuration for large datasets

### PostgreSQL

- Uses binary `COPY` command for maximum performance
- Uses `INSERT ON CONFLICT` for upserts
- Automatic handling of `SERIAL` and `BIGSERIAL` columns
- Supports UUID primary keys

## Column Mapping

The library automatically respects EF Core column mappings:

```csharp
// C# Property → Database Column
public class Example
{
    [Column("user_id")]      // Maps to: user_id
    public string UserId { get; set; }

    public string Name { get; set; }  // Maps to: Name (no attribute)
}
```

Both `[Column]` attributes and default property names work seamlessly.

## Error Handling

```csharp
try
{
    await context.BulkInsertAsync(users);
}
catch (ApplicationException ex) when (ex.InnerException is SqlException)
{
    // Handle SQL Server specific errors
    Console.WriteLine($"Database error: {ex.InnerException.Message}");
}
catch (ApplicationException ex) when (ex.InnerException is NpgsqlException)
{
    // Handle PostgreSQL specific errors
    Console.WriteLine($"Database error: {ex.InnerException.Message}");
}
catch (Exception ex)
{
    // Handle general errors
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Best Practices

1. **Use Transactions**: All bulk operations automatically run in transactions
2. **Batch Large Datasets**: For SQL Server, configure `BatchSize` for datasets > 10,000 records
3. **Configure Timeouts**: Set appropriate `BulkCopyTimeout` for large operations
4. **Validate Data**: Ensure data is validated before bulk operations
5. **Test Performance**: Benchmark with your actual data to find optimal batch sizes
6. **Connection Management**: Dispose DbContext properly to release connections

## Requirements

- .NET 8.0 or higher
- Entity Framework Core 8.0 or higher
- SQL Server 2016+ (for SQL Server support)
- PostgreSQL 12+ (for PostgreSQL support)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, questions, or contributions, please visit the GitHub repository.

## Changelog

### Version 1.0.0
- Initial release
- BulkInsert, BulkUpdate, and BulkInsertOrUpdate support
- SQL Server and PostgreSQL support
- Full [Column] attribute support
- Async/await support
- Transaction support
- Identity column handling

---

**Made with ❤️ for high-performance data operations**
