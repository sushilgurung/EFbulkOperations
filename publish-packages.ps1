# Publish NuGet Packages to NuGet.org
# Before running this script:
# 1. Get your API key from https://www.nuget.org/account/apikeys
# 2. Replace 'YOUR_API_KEY_HERE' with your actual API key

$apiKey = "YOUR_API_KEY_HERE"
$source = "https://api.nuget.org/v3/index.json"

Write-Host "Publishing Gurung.EfBulkOperations NuGet Packages..." -ForegroundColor Green

# Publish base library first
Write-Host "`nPublishing Gurung.EfBulkOperations..." -ForegroundColor Cyan
dotnet nuget push "Gurung.EfBulkOperations\bin\Release\Gurung.EfBulkOperations.1.0.0.nupkg" --api-key $apiKey --source $source

# Wait a bit for the package to be available
Write-Host "Waiting 30 seconds for package to be indexed..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Publish PostgreSQL provider
Write-Host "`nPublishing Gurung.EfBulkOperations.PostgreSql..." -ForegroundColor Cyan
dotnet nuget push "Gurung.EfBulkOperations.PostgreSql\bin\Release\Gurung.EfBulkOperations.PostgreSql.1.0.0.nupkg" --api-key $apiKey --source $source

# Publish SQL Server provider
Write-Host "`nPublishing Gurung.EfBulkOperations.SqlServer..." -ForegroundColor Cyan
dotnet nuget push "Gurung.EfBulkOperations.SqlServer\bin\Release\Gurung.EfBulkOperations.SqlServer.1.0.0.nupkg" --api-key $apiKey --source $source

Write-Host "`n✓ All packages published successfully!" -ForegroundColor Green
Write-Host "`nNote: It may take a few minutes for packages to appear on NuGet.org" -ForegroundColor Yellow
