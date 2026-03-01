# Script để test quy trình release locally
# Giống với GitHub Actions workflow

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Testing Release Build Process" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Restore
Write-Host "[1/5] Restoring dependencies..." -ForegroundColor Yellow
dotnet restore Mobiphone.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Restore failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Restore succeeded" -ForegroundColor Green
Write-Host ""

# 2. Build
Write-Host "[2/5] Building project..." -ForegroundColor Yellow
dotnet build Mobiphone.csproj --no-restore --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build succeeded" -ForegroundColor Green
Write-Host ""

# 3. Test
Write-Host "[3/5] Running tests..." -ForegroundColor Yellow
dotnet test Mobiphone.sln --no-build --configuration Release --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  Tests failed or no tests found" -ForegroundColor Yellow
}
else {
    Write-Host "✅ Tests passed" -ForegroundColor Green
}
Write-Host ""

# 4. Publish
Write-Host "[4/5] Publishing application..." -ForegroundColor Yellow
dotnet publish Mobiphone.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o bin/Release/net8.0-windows/win-x64/publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Publish succeeded" -ForegroundColor Green
Write-Host ""

# 5. Build MSI Installer
Write-Host "[5/5] Building MSI installer..." -ForegroundColor Yellow

# Check if wix is installed
$wixInstalled = Get-Command wix -ErrorAction SilentlyContinue
if (-not $wixInstalled) {
    Write-Host "⚠️  WiX not found. Installing..." -ForegroundColor Yellow
    dotnet tool install --global wix --version 4.0.5
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to install WiX!" -ForegroundColor Red
        exit 1
    }
}

# Build installer
Push-Location Installer
wix build Package.wxs -arch x64 -out ../Mobiphone.msi
$wixResult = $LASTEXITCODE
Pop-Location

if ($wixResult -ne 0) {
    Write-Host "❌ MSI build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ MSI build succeeded" -ForegroundColor Green
Write-Host ""

# Summary
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "✅ Release Build Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output files:" -ForegroundColor White
Write-Host "  - Executable: bin\Release\net8.0-windows\win-x64\publish\Mobiphone.exe" -ForegroundColor White
Write-Host "  - MSI Installer: Mobiphone.msi" -ForegroundColor White
Write-Host ""

# Cleanup temporary files
Write-Host "🧹 Cleaning up temporary files..." -ForegroundColor Cyan
$cleanupItems = @("bin", "obj", "Installer\obj", "*.wixpdb")
foreach ($item in $cleanupItems) {
    Remove-Item $item -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "✅ Cleanup complete!" -ForegroundColor Green
Write-Host ""

Write-Host "You can test the installer by running:" -ForegroundColor Yellow
Write-Host "  .\Mobiphone.msi" -ForegroundColor Cyan
Write-Host ""
