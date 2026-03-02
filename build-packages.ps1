# Build NuGet Packages for Gurung.EfBulkOperations
# This script builds all three NuGet packages with proper dependencies

Write-Host "Building Gurung.EfBulkOperations NuGet Packages..." -ForegroundColor Green

# Clean previous builds
Write-Host "`nCleaning previous builds..." -ForegroundColor Yellow
dotnet clean -c Release

# Build the solution
Write-Host "`nBuilding solution..." -ForegroundColor Cyan
dotnet build Gurung.EfBulkOperations.sln -c Release /p:WarningLevel=0

# Build and pack base library first
Write-Host "`nPacking Gurung.EfBulkOperations (Base Library)..." -ForegroundColor Cyan
dotnet pack "Gurung.EfBulkOperations\Gurung.EfBulkOperations.csproj" -c Release --no-build

# Build and pack PostgreSQL provider
Write-Host "`nPacking Gurung.EfBulkOperations.PostgreSql..." -ForegroundColor Cyan
dotnet pack "Gurung.EfBulkOperations.PostgreSql\Gurung.EfBulkOperations.PostgreSql.csproj" -c Release --no-build

# Build and pack SQL Server provider
Write-Host "`nPacking Gurung.EfBulkOperations.SqlServer..." -ForegroundColor Cyan
dotnet pack "Gurung.EfBulkOperations.SqlServer\Gurung.EfBulkOperations.SqlServer.csproj" -c Release --no-build

Write-Host "`nAll packages built successfully!" -ForegroundColor Green
Write-Host "`nPackages location:" -ForegroundColor Yellow
Write-Host "  - Gurung.EfBulkOperations\bin\Release\Gurung.EfBulkOperations.1.0.0.nupkg"
Write-Host "  - Gurung.EfBulkOperations.PostgreSql\bin\Release\Gurung.EfBulkOperations.PostgreSql.1.0.0.nupkg"
Write-Host "  - Gurung.EfBulkOperations.SqlServer\bin\Release\Gurung.EfBulkOperations.SqlServer.1.0.0.nupkg"
