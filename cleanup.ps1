# Script để cleanup các file tạm thời và build artifacts
# Chạy sau khi đã có file MSI

Write-Host "🧹 Cleaning up temporary files..." -ForegroundColor Cyan
Write-Host ""

# Files và folders cần xóa
$itemsToRemove = @(
    "bin",
    "obj", 
    "Installer\obj",
    "Installer\bin",
    "*.wixpdb",
    "*.wixobj",
    "*-test*.msi"
)

$removedCount = 0
$totalSize = 0

foreach ($item in $itemsToRemove) {
    $matches = Get-Item $item -ErrorAction SilentlyContinue
    if ($matches) {
        foreach ($match in $matches) {
            if ($match.PSIsContainer) {
                $size = (Get-ChildItem $match.FullName -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
                $totalSize += $size
                Write-Host "  🗑️  Removing folder: $($match.Name) ($([math]::Round($size/1MB,2)) MB)" -ForegroundColor Yellow
            } else {
                $totalSize += $match.Length
                Write-Host "  🗑️  Removing file: $($match.Name) ($([math]::Round($match.Length/1MB,2)) MB)" -ForegroundColor Yellow
            }
            Remove-Item $match.FullName -Recurse -Force -ErrorAction SilentlyContinue
            $removedCount++
        }
    }
}

Write-Host ""
if ($removedCount -gt 0) {
    Write-Host "✅ Cleanup complete!" -ForegroundColor Green
    Write-Host "   Removed: $removedCount items" -ForegroundColor White
    Write-Host "   Freed: $([math]::Round($totalSize/1MB,2)) MB" -ForegroundColor White
} else {
    Write-Host "✅ Nothing to clean!" -ForegroundColor Green
}

Write-Host ""
Write-Host "📦 Final MSI file:" -ForegroundColor Cyan
Get-ChildItem "*.msi" -Exclude "*-test*" | Select-Object Name, @{Name="Size (MB)";Expression={[math]::Round($_.Length/1MB,2)}}, LastWriteTime | Format-Table -AutoSize
