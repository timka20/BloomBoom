$ErrorActionPreference = 'Stop'
$bloomRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$bloomBuild = Join-Path $bloomRoot 'Builds/BloomBoom3D-v2'
$bloomExe = Get-Item -LiteralPath (Join-Path $bloomBuild 'BloomBoom.exe')
$bloomReport = Get-Item -LiteralPath (Join-Path $bloomBuild 'QA/PASS.txt')
$bloomLog = Get-Content -LiteralPath (Join-Path $bloomRoot 'Logs/player-v2.log') -Raw
if ($bloomReport.LastWriteTime -lt $bloomExe.LastWriteTime) { throw 'QA report is older than this executable.' }
if ($bloomLog -notmatch 'BLOOM_SELF_TEST_PASS_V2' -or $bloomLog -match 'BLOOM_SELF_TEST_FAIL|NullReferenceException|ArgumentException') { throw 'Player QA has not passed.' }

# Extract the actual executable resource for visual verification, not a substitute icon.
Add-Type -AssemblyName System.Drawing
$bloomIcon = [System.Drawing.Icon]::ExtractAssociatedIcon($bloomExe.FullName)
if ($null -eq $bloomIcon) { throw 'Executable has no application icon.' }
$bloomBitmap = $bloomIcon.ToBitmap()
$bloomBitmap.Save((Join-Path $bloomBuild 'QA/11-embedded-exe-icon.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$bloomBitmap.Dispose()
$bloomIcon.Dispose()

Copy-Item -LiteralPath (Join-Path $bloomRoot 'README.md') -Destination (Join-Path $bloomBuild 'README.md')
Copy-Item -LiteralPath (Join-Path $bloomRoot 'ASSET_CREDITS.md') -Destination (Join-Path $bloomBuild 'ASSET_CREDITS.md')
$bloomFiles = Get-ChildItem -LiteralPath $bloomBuild | Where-Object { $_.Name -ne 'QA' -and $_.Name -notlike '*BackUpThisFolder*' }
$bloomZip = Join-Path $bloomRoot 'Builds/BloomBoom3D-v2-Windows.zip'
Compress-Archive -LiteralPath $bloomFiles.FullName -DestinationPath $bloomZip -CompressionLevel Optimal -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$bloomArchive = [System.IO.Compression.ZipFile]::OpenRead($bloomZip)
try {
    $bloomEntries = @($bloomArchive.Entries | ForEach-Object { $_.FullName.Replace('\','/') })
    foreach ($bloomRequired in @('BloomBoom.exe','UnityPlayer.dll','BloomBoom_Data/globalgamemanagers')) {
        if ($bloomRequired -notin $bloomEntries) { throw "Missing runtime file: $bloomRequired" }
    }
    Write-Output "Verified archive entries: $($bloomArchive.Entries.Count)"
} finally { $bloomArchive.Dispose() }
Write-Output "Passed checks: $(([regex]::Matches($bloomLog, 'CHECK_PASS:')).Count)"
Get-Item -LiteralPath $bloomExe.FullName,$bloomZip | Select-Object FullName,Length,LastWriteTime
