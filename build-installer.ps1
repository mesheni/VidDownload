param(
    [string]$Configuration = "Release",
    [string]$PublishDir = "publish",
    [string]$OutputFile = "VidDownload.msi"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== VidDownload MSI Installer Build ===" -ForegroundColor Cyan

Write-Host "`n[1/5] Publishing VidDownload.WPF ($Configuration)..." -ForegroundColor Yellow
$project = Join-Path $repoRoot "VidDownload.WPF\VidDownload.WPF.csproj"
$updaterProject = Join-Path $repoRoot "Updater\Updater.csproj"
$publishPath = Join-Path $repoRoot $PublishDir
dotnet publish $project -c $Configuration -o $publishPath --nologo 2>&1 | Out-Host
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "`n[2/5] Building Updater..." -ForegroundColor Yellow
$updaterOut = Join-Path $repoRoot "Updater\bin\Release\net10.0\win-x64\publish"
dotnet publish $updaterProject -c Release --self-contained -p:PublishSingleFile=true -r win-x64 -o $updaterOut --nologo 2>&1 | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Updater build failed" }
Copy-Item (Join-Path $updaterOut "Updater.exe") (Join-Path $publishPath "Updater.exe") -Force
Write-Host "    Updater.exe included"

Write-Host "`n[3/5] Generating Files.wxs from publish output..." -ForegroundColor Yellow
$files = Get-ChildItem -Path $publishPath -Recurse -File
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
[void]$sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
[void]$sb.AppendLine('    <Fragment>')
[void]$sb.AppendLine('        <ComponentGroup Id="PublishedFiles" Directory="INSTALLFOLDER">')

$publishLen = $publishPath.Length + 1
$dirMap = @{ "en" = "dir_en"; "ru" = "dir_ru"; "zh-CN" = "dir_zh_CN" }
foreach ($file in $files) {
    $relPath = $file.FullName.Substring($publishLen)
    $id = "pub_" + $relPath -replace '[^a-zA-Z0-9_.]', '_'
    $sourcePath = "!(bindpath.publish)\$relPath"
    $guid = [System.Guid]::NewGuid()
    $dirAttr = ""
    $parts = $relPath.Split([IO.Path]::DirectorySeparatorChar, 2)
    if ($parts.Count -eq 2 -and $dirMap.ContainsKey($parts[0])) {
        $dirAttr = " Directory=`"$($dirMap[$parts[0]])`""
    }
    [void]$sb.AppendLine("            <Component Guid=`"$guid`"$dirAttr>")
    [void]$sb.AppendLine("                <File Id=`"$id`" Source=`"$sourcePath`" KeyPath=`"yes`" />")
    [void]$sb.AppendLine("            </Component>")
}
[void]$sb.AppendLine('        </ComponentGroup>')
[void]$sb.AppendLine('    </Fragment>')
[void]$sb.AppendLine('</Wix>')

$filesWxs = Join-Path $repoRoot "Files.wxs"
Set-Content -Path $filesWxs -Value $sb.ToString() -Encoding UTF8
Write-Host "    Generated $($files.Count) file entries -> Files.wxs"

Write-Host "`n[4/5] Building MSI..." -ForegroundColor Yellow
$setupWxs = Join-Path $repoRoot "Setup.wxs"
$msiPath = Join-Path $repoRoot $OutputFile

$exePath = Join-Path $publishPath "VidDownload.WPF.exe"
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath).FileVersion
$msiVersion = ($version -split '\.')[0..2] -join '.'
Write-Host "    Version: $msiVersion"

wix build $setupWxs $filesWxs -bindpath "publish=$publishPath" -d "Version=$msiVersion" -o $msiPath 2>&1
if ($LASTEXITCODE -ne 0) { throw "wix build failed" }

Write-Host "`n[5/5] Cleaning up intermediate files..." -ForegroundColor Yellow
Remove-Item -Path $filesWxs -Force

Write-Host "`n=== SUCCESS ===" -ForegroundColor Green
Write-Host "MSI: $msiPath"
Write-Host "Size: $((Get-Item $msiPath).Length / 1KB) KB"
