# Script to generate WiX components from publish directory
# This creates a HarvestedFiles.wxs with all files as individual components

param(
    [Parameter(Mandatory=$true)]
    [string]$PublishPath,
    
    [Parameter(Mandatory=$true)]
    [string]$OutputFile
)

function New-Guid {
    return [guid]::NewGuid().ToString().ToUpper()
}

function Get-SafeId {
    param([string]$relativePath)
    # Use full relative path to make unique IDs
    # Remove invalid characters and ensure it starts with a letter
    $id = $relativePath -replace '[^a-zA-Z0-9_]', '_'
    if ($id -match '^[0-9]') {
        $id = "File_$id"
    }
    # Limit length to 72 characters (WiX limit)
    if ($id.Length > 72) {
        $hash = ($id | Get-FileHash -Algorithm MD5).Hash.Substring(0, 8)
        $id = $id.Substring(0, 60) + "_" + $hash
    }
    return $id
}

if (-not (Test-Path $PublishPath)) {
    Write-Host "❌ Publish path not found: $PublishPath" -ForegroundColor Red
    exit 1
}

$files = Get-ChildItem -Path $PublishPath -File -Recurse

Write-Host "Generating WiX components for $($files.Count) files..." -ForegroundColor Cyan

$xmlHeader = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="HarvestedFiles" Directory="INSTALLFOLDER">
"@

$xmlFooter = @"
    </ComponentGroup>
  </Fragment>
</Wix>
"@

$components = @()

foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($PublishPath.Length + 1)
    $fileId = Get-SafeId $relativePath
    $componentId = "Cmp_$fileId"
    
    # Ensure unique component IDs
    $counter = 1
    $originalComponentId = $componentId
    while ($components -contains $componentId) {
        $componentId = "${originalComponentId}_$counter"
        $counter++
    }
    $components += $componentId
    
    $guid = New-Guid
    
    # Decide KeyPath - main exe should be one
    $keyPath = if ($file.Name -eq "Mobiphone.exe") { "yes" } else { "yes" }
    
    $xmlHeader += @"

      <Component Id="$componentId" Guid="$guid">
        <File Id="$fileId" Source="`$(var.PublishDir)\$relativePath" KeyPath="$keyPath" />
      </Component>
"@
}

$xml = $xmlHeader + $xmlFooter

# Write to output file
$xml | Out-File -FilePath $OutputFile -Encoding UTF8

Write-Host "✅ Generated $($files.Count) components in $OutputFile" -ForegroundColor Green
