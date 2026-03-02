# NuGet Package Complete Guide - Gurung.EfBulkOperations

## Package Overview

All **Gurung.EfBulkOperations** projects have been configured and built as NuGet packages with automatic dependency management.

### Package Details

| Package Name | Version | Location |
|-------------|---------|----------|
| **Gurung.EfBulkOperations** | 1.0.0 | `Gurung.EfBulkOperations\bin\Release\` |
| **Gurung.EfBulkOperations.SqlServer** | 1.0.0 | `Gurung.EfBulkOperations.SqlServer\bin\Release\` |
| **Gurung.EfBulkOperations.PostgreSql** | 1.0.0 | `Gurung.EfBulkOperations.PostgreSql\bin\Release\` |

### ✅ Automatic Dependency Configuration

**CONFIGURED**: When users install `Gurung.EfBulkOperations.PostgreSql` or `Gurung.EfBulkOperations.SqlServer`, the base `Gurung.EfBulkOperations` package will be **automatically installed** as a dependency.

### Package Metadata

Each package includes:

#### Common Properties
- ✅ **Version**: 1.0.0
- ✅ **Authors**: Sushil Gurung
- ✅ **Company**: Gurung
- ✅ **License**: MIT
- ✅ **Project URL**: https://github.com/sushilgurung/bulkOperations
- ✅ **Repository**: https://github.com/sushilgurung/bulkOperations (git)
- ✅ **README.md**: Included in all packages
- ✅ **XML Documentation**: Auto-generated for IntelliSense
- ✅ **Auto-build**: Packages automatically generated on build

#### Package-Specific Descriptions

**Gurung.EfBulkOperations (Core)**
> High-performance bulk operations library for Entity Framework Core. Core library providing base functionality for SQL Server and PostgreSQL bulk operations.

**Gurung.EfBulkOperations.SqlServer**
> SQL Server provider for Gurung.EfBulkOperations. High-performance bulk operations using SqlBulkCopy and MERGE statements for SQL Server 2016 and above.
> **Dependencies**: Gurung.EfBulkOperations (auto-installed) ✅

**Gurung.EfBulkOperations.PostgreSql**
> PostgreSQL provider for Gurung.EfBulkOperations. High-performance bulk operations using binary COPY command and INSERT ON CONFLICT for PostgreSQL 12 and above.
> **Dependencies**: Gurung.EfBulkOperations (auto-installed) ✅

### Package Tags

All packages include searchable tags:
- `entityframework`
- `efcore`
- `bulk`
- `bulkinsert`
- `bulkupdate`
- `upsert`
- `performance`
- `sqlserver` (SqlServer package)
- `postgresql`, `postgres`, `npgsql`, `copy` (PostgreSQL package)
- `merge` (SqlServer package)

---

## Quick Start - Building & Publishing

Three helper scripts have been created for you:

### 📦 1. build-packages.ps1
Builds all three NuGet packages in the correct order.

```powershell
.\build-packages.ps1
```

### 🧪 2. test-local-packages.ps1
Sets up a local NuGet feed for testing packages before publishing.

```powershell
.\test-local-packages.ps1
```

### 🚀 3. publish-packages.ps1
Publishes all packages to NuGet.org (requires API key configuration).

```powershell
# First, edit the script to add your API key
# Then run:
.\publish-packages.ps1
```

---

## Step-by-Step Guide

### Step 1: Build the Packages

**Option A: Using the Script (Recommended)**
```powershell
.\build-packages.ps1
```

**Option B: Manual Build**
```powershell
# Build in Release configuration
dotnet build -c Release

# Or build specific projects
dotnet build Gurung.EfBulkOperations\Gurung.EfBulkOperations.csproj -c Release
```

Packages are auto-generated in each project's `bin\Release\` folder.

### Step 2: Test Locally (Recommended)

Run the test script:
```powershell
.\test-local-packages.ps1
```

Then test in a new project:
```powershell
# Create a test project
mkdir TestApp
cd TestApp
dotnet new console

# Install from local feed
dotnet add package Gurung.EfBulkOperations.PostgreSql --source GurungBulkOpsLocal --version 1.0.0

# Verify dependencies
dotnet list package
```

You should see:
- ✅ Gurung.EfBulkOperations.PostgreSql (1.0.0)
- ✅ Gurung.EfBulkOperations (1.0.0) ← **Automatically installed!**

### Step 3: Publish to NuGet.org

#### Get Your NuGet API Key
1. Go to https://www.nuget.org/
2. Sign in or create an account
3. Go to your account settings
4. Navigate to "API Keys"
5. Create a new API key with "Push" permissions

#### Publish Using the Script (Recommended)

1. Open `publish-packages.ps1`
2. Replace `YOUR_API_KEY_HERE` with your actual API key
3. Run the script:
   ```powershell
   .\publish-packages.ps1
   ```

#### Publish Manually

```powershell
# Set your API key
$apiKey = "YOUR_API_KEY"

# Publish base package FIRST
dotnet nuget push "Gurung.EfBulkOperations\bin\Release\Gurung.EfBulkOperations.1.0.0.nupkg" `
    --api-key $apiKey `
    --source https://api.nuget.org/v3/index.json

# Wait 30 seconds for indexing
Start-Sleep -Seconds 30

# Publish SQL Server provider
dotnet nuget push "Gurung.EfBulkOperations.SqlServer\bin\Release\Gurung.EfBulkOperations.SqlServer.1.0.0.nupkg" `
    --api-key $apiKey `
    --source https://api.nuget.org/v3/index.json

# Publish PostgreSQL provider
dotnet nuget push "Gurung.EfBulkOperations.PostgreSql\bin\Release\Gurung.EfBulkOperations.PostgreSql.1.0.0.nupkg" `
    --api-key $apiKey `
    --source https://api.nuget.org/v3/index.json
```

**Important**: Always publish the base package first, then wait for it to be indexed before publishing dependent packages.

### Step 4: Verify Publication

1. Wait 5-10 minutes for complete indexing
2. Search for "Gurung.EfBulkOperations" on https://www.nuget.org/
3. Verify all packages appear
4. Check that dependencies are correctly listed

---

## Package Dependencies

```
Gurung.EfBulkOperations (Core)
├── Microsoft.EntityFrameworkCore.Relational (8.0.7)
│
├── Gurung.EfBulkOperations.SqlServer
│   ├── Gurung.EfBulkOperations (1.0.0) ✅ Auto-installed
│   └── Microsoft.Data.SqlClient (6.0.1)
│
└── Gurung.EfBulkOperations.PostgreSql
    ├── Gurung.EfBulkOperations (1.0.0) ✅ Auto-installed
    └── Npgsql.EntityFrameworkCore.PostgreSQL (8.0.4)
```

### How Dependencies Work

The PostgreSQL and SQL Server projects have `ProjectReference` configured with `<PrivateAssets>none</PrivateAssets>`:

```xml
<ProjectReference Include="..\Gurung.EfBulkOperations\Gurung.EfBulkOperations.csproj">
  <PrivateAssets>none</PrivateAssets>
</ProjectReference>
```

This ensures that:
- ✅ When users install `Gurung.EfBulkOperations.PostgreSql`, they automatically get `Gurung.EfBulkOperations`
- ✅ When users install `Gurung.EfBulkOperations.SqlServer`, they automatically get `Gurung.EfBulkOperations`
- ✅ No manual installation of the base package is required

---

## User Installation

Once published, users install with:

```bash
# For SQL Server (base package installs automatically)
dotnet add package Gurung.EfBulkOperations.SqlServer

# For PostgreSQL (base package installs automatically)
dotnet add package Gurung.EfBulkOperations.PostgreSql

# Core library only (if needed)
dotnet add package Gurung.EfBulkOperations
```

---

## Updating Package Version

To release a new version:

1. **Update version in ALL .csproj files**:
   ```xml
   <Version>1.0.1</Version>
   ```

2. **Rebuild packages**:
   ```powershell
   .\build-packages.ps1
   ```

3. **Test locally** (optional but recommended):
   ```powershell
   .\test-local-packages.ps1
   ```

4. **Publish to NuGet.org**:
   ```powershell
   .\publish-packages.ps1
   ```

---

## Verifying Dependencies in Packages

To inspect a package's dependencies:

```powershell
# Extract the .nupkg (it's a zip file)
Expand-Archive "Gurung.EfBulkOperations.PostgreSql\bin\Release\Gurung.EfBulkOperations.PostgreSql.1.0.0.nupkg" -DestinationPath "temp"

# View the .nuspec file
Get-Content "temp\Gurung.EfBulkOperations.PostgreSql.nuspec"
```

Look for the `<dependencies>` section - you should see:
```xml
<dependency id="Gurung.EfBulkOperations" version="1.0.0" />
```

---

## Troubleshooting

### Issue: Dependencies Not Showing Up
**Solution**:
- Ensure `<PrivateAssets>none</PrivateAssets>` is set on ProjectReference ✅ (Already configured)
- Rebuild the packages
- Check the generated .nuspec file inside the .nupkg

### Issue: Package Not Updating
**Solution**:
```powershell
# Clear NuGet caches
dotnet nuget locals all --clear

# Delete bin and obj folders
Remove-Item -Recurse -Force */bin, */obj

# Rebuild
dotnet build -c Release
```

### Issue: Can't Publish to NuGet.org
**Solution**:
- Verify API key is correct and has push permissions
- Ensure package ID is unique (not already taken)
- Check all required metadata is present
- Ensure base package is published and indexed before dependent packages

### Issue: "Package Already Exists"
**Solution**:
- You cannot overwrite an existing version
- Increment the version number in all .csproj files
- Rebuild and republish

---

## Local Testing Alternative

To test without the script:

```powershell
# Create local feed
dotnet nuget add source "d:\Nuget_Packages\BulkInsert\Gurung.BulkOperations" -n "LocalGurung"

# Use in test project
dotnet add package Gurung.EfBulkOperations.SqlServer --version 1.0.0 --source LocalGurung
```

---

## Build Configuration

All projects configured with:
- ✅ Target Framework: .NET 8.0
- ✅ Implicit Usings: Enabled
- ✅ Nullable: Disabled
- ✅ Generate Package on Build: True
- ✅ Generate XML Documentation: True
- ✅ README.md: Included
- ✅ License: MIT
- ✅ Repository: GitHub

---

## Additional Resources

- **NuGet Documentation**: https://docs.microsoft.com/en-us/nuget/
- **Semantic Versioning**: https://semver.org/
- **NuGet Best Practices**: https://docs.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices
- **API Key Management**: https://www.nuget.org/account/apikeys

---

**Last Updated**: February 19, 2026  
**Status**: ✅ Configured with automatic dependencies  
**Total Packages**: 3 (Base + 2 Providers)
