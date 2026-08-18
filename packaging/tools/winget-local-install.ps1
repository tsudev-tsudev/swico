<#
    winget-local-install.ps1
    Cai tsudev SWICO bang winget NGAY BAY GIO, khong can cho Microsoft duyet.

    Vi sao can script nay: "winget install tsudev.SWICO" chi chay duoc sau khi
    manifest duoc hop nhat vao kho cong dong microsoft/winget-pkgs - viec do can
    kiem thu tu dong va nguoi duyet, mat vai gio toi vai ngay.

    Nhung winget CO san kha nang cai tu manifest cuc bo. Script nay tai manifest
    da duoc sinh san trong ban phat hanh, kiem tra roi cai.

    Cach dung (PowerShell, quyen Administrator):
        .\winget-local-install.ps1
        .\winget-local-install.ps1 -Version 26.8.18.2
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$Repo = 'tsudev-tsudev/swico',
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'

if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    throw 'Khong tim thay winget. Cai "App Installer" tu Microsoft Store truoc.'
}

# Cai tu manifest cuc bo doi hoi bat che do nha phat trien cua winget.
$settings = winget settings export | ConvertFrom-Json
if (-not $settings.adminSettings.LocalManifestFiles) {
    Write-Host 'Dang bat che do cho phep cai tu manifest cuc bo...' -ForegroundColor Cyan
    Write-Host '(can quyen Administrator - winget se hoi neu chua co)' -ForegroundColor DarkGray
    winget settings --enable LocalManifestFiles
}

$tag = if ($Version) { "v$Version" } else { 'latest' }
$api = if ($Version) { "https://api.github.com/repos/$Repo/releases/tags/$tag" }
       else           { "https://api.github.com/repos/$Repo/releases/latest" }

Write-Host "Dang doc ban phat hanh $tag ..." -ForegroundColor Cyan
$release = Invoke-RestMethod -Uri $api -Headers @{ 'User-Agent' = 'swico-winget-local' }
$asset = $release.assets | Where-Object { $_.name -like 'winget-manifest-*.zip' } | Select-Object -First 1
if (-not $asset) {
    throw "Ban phat hanh $($release.tag_name) khong kem goi manifest winget."
}

$work = Join-Path $env:TEMP "swico-winget-$($release.tag_name)"
if (Test-Path $work) { Remove-Item $work -Recurse -Force }
New-Item -ItemType Directory -Path $work | Out-Null

$zip = Join-Path $work $asset.name
Write-Host "Dang tai $($asset.name) ..." -ForegroundColor Cyan
Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip
Expand-Archive -Path $zip -DestinationPath $work -Force

$dir = (Get-ChildItem -Recurse $work -Filter '*.installer.yaml' | Select-Object -First 1).Directory.FullName
if (-not $dir) { throw 'Khong tim thay manifest trong goi da tai.' }
Write-Host "Manifest: $dir" -ForegroundColor DarkGray

Write-Host "`nDang kiem tra manifest..." -ForegroundColor Cyan
winget validate --manifest $dir
if ($LASTEXITCODE -ne 0) { throw 'Manifest KHONG hop le - dung lai.' }

if ($ValidateOnly) {
    Write-Host "`nManifest hop le. Bo qua buoc cai (-ValidateOnly)." -ForegroundColor Green
    return
}

Write-Host "`nDang cai dat..." -ForegroundColor Cyan
# winget tu doi chieu InstallerSha256 trong manifest voi file tai ve.
winget install --manifest $dir --accept-package-agreements
if ($LASTEXITCODE -ne 0) { throw "winget install that bai (ma $LASTEXITCODE)." }

Write-Host "`nXong. Thu chay:  swico --version" -ForegroundColor Green
