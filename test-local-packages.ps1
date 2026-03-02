# Test NuGet Packages Locally
# This script helps you test the packages locally before publishing

Write-Host "Setting up local NuGet package testing..." -ForegroundColor Green

# Create a local NuGet feed directory
$localFeed = "$PSScriptRoot\LocalNuGetFeed"
if (-not (Test-Path $localFeed)) {
    New-Item -ItemType Directory -Path $localFeed | Out-Null
    Write-Host "Created local feed directory: $localFeed" -ForegroundColor Yellow
}

# Copy packages to local feed
Write-Host "`nCopying packages to local feed..." -ForegroundColor Cyan
Copy-Item "Gurung.EfBulkOperations\bin\Release\*.nupkg" $localFeed -Force
Copy-Item "Gurung.EfBulkOperations.PostgreSql\bin\Release\*.nupkg" $localFeed -Force
Copy-Item "Gurung.EfBulkOperations.SqlServer\bin\Release\*.nupkg" $localFeed -Force

Write-Host "`n✓ Packages copied to: $localFeed" -ForegroundColor Green

# Add local source if not already added
Write-Host "`nConfiguring NuGet sources..." -ForegroundColor Cyan
$sourceName = "GurungBulkOpsLocal"
$existingSource = dotnet nuget list source | Select-String $sourceName

if (-not $existingSource) {
    dotnet nuget add source $localFeed --name $sourceName
    Write-Host "✓ Added local source: $sourceName" -ForegroundColor Green
} else {
    Write-Host "Local source already exists: $sourceName" -ForegroundColor Yellow
}

Write-Host "`n=====================================" -ForegroundColor White
Write-Host "Test Installation Commands:" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor White
Write-Host "dotnet add package Gurung.EfBulkOperations.PostgreSql --source $sourceName --version 1.0.0"
Write-Host "dotnet add package Gurung.EfBulkOperations.SqlServer --source $sourceName --version 1.0.0"
Write-Host "`nTo remove the local source later:" -ForegroundColor Yellow
Write-Host "dotnet nuget remove source $sourceName"
