# Script to generate WiX components from publish directory
# This creates a HarvestedFiles.wxs with all files as individual components

param(
    [Parameter(Mandatory=$true)]
    [string]$PublishPath,
    
    [Parameter(Mandatory=$true)]
    [string]$OutputFile
)

function Get-SafeId {
    param([string]$relativePath)
    # Use full relative path to make unique IDs
    # Remove invalid characters and ensure it starts with a letter
    $id = $relativePath -replace '[^a-zA-Z0-9_]', '_'
    if ($id -match '^[0-9]') {
        $id = "File_$id"
    }
    # Limit length to 72 characters (WiX limit)
    if ($id.Length -gt 72) {
        $hash = (Get-StableGuid $id).Replace('-', '').Substring(0, 8)
        $id = $id.Substring(0, 60) + "_" + $hash
    }
    return $id
}

function Get-StableGuid {
    param([string]$value)

    $md5 = [System.Security.Cryptography.MD5]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($value.ToLowerInvariant())
        $hash = $md5.ComputeHash($bytes)
        return ([guid]::new($hash)).ToString().ToUpper()
    }
    finally {
        $md5.Dispose()
    }
}

function ConvertTo-XmlText {
    param([string]$value)
    return [System.Security.SecurityElement]::Escape($value)
}

if (-not (Test-Path $PublishPath)) {
    Write-Host "Publish path not found: $PublishPath" -ForegroundColor Red
    exit 1
}

$files = Get-ChildItem -Path $PublishPath -File -Recurse

Write-Host "Generating WiX components for $($files.Count) files..." -ForegroundColor Cyan

$directories = @{}
foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($PublishPath.Length + 1)
    $relativeDirectory = [System.IO.Path]::GetDirectoryName($relativePath)

    while (-not [string]::IsNullOrWhiteSpace($relativeDirectory)) {
        if (-not $directories.ContainsKey($relativeDirectory)) {
            $directories[$relativeDirectory] = "Dir_$(Get-SafeId $relativeDirectory)"
        }
        $relativeDirectory = [System.IO.Path]::GetDirectoryName($relativeDirectory)
    }
}

$directoryXml = ""
foreach ($relativeDirectory in ($directories.Keys | Sort-Object { ($_ -split '[\\/]').Count }, { $_ })) {
    $parentPath = [System.IO.Path]::GetDirectoryName($relativeDirectory)
    $parentId = if ([string]::IsNullOrWhiteSpace($parentPath)) { "INSTALLFOLDER" } else { $directories[$parentPath] }
    $directoryId = $directories[$relativeDirectory]
    $directoryName = ConvertTo-XmlText ([System.IO.Path]::GetFileName($relativeDirectory))

    $directoryXml += @"
    <DirectoryRef Id="$parentId">
      <Directory Id="$directoryId" Name="$directoryName" />
    </DirectoryRef>
"@
}

$xmlHeader = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
$directoryXml  </Fragment>
  <Fragment>
    <ComponentGroup Id="HarvestedFiles">
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
    $relativeDirectory = [System.IO.Path]::GetDirectoryName($relativePath)
    $directoryId = if ([string]::IsNullOrWhiteSpace($relativeDirectory)) { "INSTALLFOLDER" } else { $directories[$relativeDirectory] }
    
    # Ensure unique component IDs
    $counter = 1
    $originalComponentId = $componentId
    while ($components -contains $componentId) {
        $componentId = "${originalComponentId}_$counter"
        $counter++
    }
    $components += $componentId
    
    $guid = Get-StableGuid "SC Hoang Quoc|$relativePath"
    $sourcePath = ConvertTo-XmlText $relativePath
    
    $xmlHeader += @"

      <Component Id="$componentId" Directory="$directoryId" Guid="$guid">
        <File Id="$fileId" Source="`$(var.PublishDir)\$sourcePath" KeyPath="yes" />
      </Component>
"@
}

$xml = $xmlHeader + $xmlFooter

# Write to output file
$xml | Out-File -FilePath $OutputFile -Encoding UTF8

Write-Host "Generated $($files.Count) components in $OutputFile" -ForegroundColor Green
