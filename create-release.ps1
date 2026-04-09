# create-release.ps1
$src = "S:\Github\Completionist GUI Patcher\bin\Release\net8.0-windows"
$dest = "S:\Github\Completionist GUI Patcher\Completionist.GUI.Patcher.zip"
$excludeFolder = "ffdec"
$excludeFile = "update.bat"

if (-not (Test-Path $src)) {
    Write-Error "Source folder not found: $src"
    exit 1
}

# Remove old zip if it exists
if (Test-Path $dest) { Remove-Item $dest }

Write-Host "Creating $dest from $src (excluding $excludeFolder and $excludeFile)..."

# Temporary folder to stage files
$tempDir = Join-Path $env:TEMP "ReleaseTemp"
if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
New-Item -ItemType Directory -Path $tempDir | Out-Null

# Copy all files except excluded folder/file, preserving relative paths
Get-ChildItem -Path $src -Recurse -File | Where-Object {
    ($_.FullName -notmatch [regex]::Escape("\$excludeFolder\")) -and
    ($_.Name -ne $excludeFile)
} | ForEach-Object {
    $relative = $_.FullName.Substring($src.Length + 1)
    $target = Join-Path $tempDir $relative
    $parent = Split-Path $target
    if (-not (Test-Path $parent)) { New-Item -ItemType Directory -Path $parent | Out-Null }
    Copy-Item $_.FullName $target
}

# Compress everything in temp folder into the zip
Compress-Archive -Path "$tempDir\*" -DestinationPath $dest -CompressionLevel Optimal

# Clean up
Remove-Item $tempDir -Recurse -Force

Write-Host "Done! Created $dest (GitHub-ready)"