<#
    build.ps1 - Build, test va dong goi tsuowlit SWICO

    Chay tren Windows co .NET 8 SDK. Cung chay duoc tren Linux/macOS bang
    pwsh - ke ca buoc publish win-x64 (cross-compile duoc), chi khong chay
    thu duoc file exe.

    Vi du:
        .\build.ps1                  # test + build + publish
        .\build.ps1 -SkipTests       # bo qua test (chi khi dang go loi)
        .\build.ps1 -Package         # publish xong thi dong goi installer
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [switch]$SkipTests,
    [switch]$Package
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

# Phien ban doc TU Directory.Build.props - nguon su that duy nhat. Khong viet
# cung o day, vi so hieu nay con di vao installer va winget manifest.
$version = ([xml](Get-Content Directory.Build.props)).Project.PropertyGroup.VersionPrefix |
           Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw 'Khong doc duoc VersionPrefix tu Directory.Build.props' }
Write-Host "tsuowlit SWICO $version" -ForegroundColor Cyan

# --- 1/4 test ---------------------------------------------------------------
if ($SkipTests) {
    Write-Host "`n[1/4] Bo qua test (-SkipTests)." -ForegroundColor DarkYellow
} else {
    Write-Host "`n[1/4] Chay unit test (Core - khong can Windows API)" -ForegroundColor Cyan
    dotnet run --project tests/unittests -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Unit test THAT BAI - dung build.' }
}

# --- 2/4 build --------------------------------------------------------------
Write-Host "`n[2/4] Build toan bo solution" -ForegroundColor Cyan
dotnet build Tsudev.SystemAudit.sln -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'Build THAT BAI.' }

# --- 3/4 publish ------------------------------------------------------------
Write-Host "`n[3/4] Publish thanh 1 file .exe duy nhat" -ForegroundColor Cyan
dotnet publish src/Tsudev.Audit.Cli -c $Configuration -r win-x64 -o publish
if ($LASTEXITCODE -ne 0) { throw 'Publish THAT BAI.' }

$exe = Join-Path $PSScriptRoot 'publish/swico.exe'
if (-not (Test-Path $exe)) { throw "Khong tim thay $exe sau khi publish." }
$sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host "  -> $exe ($sizeMb MB)" -ForegroundColor Green

# --- 4/4 dong goi -----------------------------------------------------------
if ($Package) {
    Write-Host "`n[4/4] Dong goi installer (Inno Setup)" -ForegroundColor Cyan
    $iscc = Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'
    if (-not (Test-Path $iscc)) {
        throw "Khong tim thay Inno Setup 6 tai $iscc. Tai tai https://jrsoftware.org/isdl.php"
    }
    & $iscc "/DAppVersion=$version" packaging\innosetup\swico.iss
    if ($LASTEXITCODE -ne 0) { throw 'Dong goi installer THAT BAI.' }

    Get-ChildItem packaging/output -Filter *.exe | ForEach-Object {
        Write-Host "  -> $($_.FullName)" -ForegroundColor Green
    }
} else {
    Write-Host "`n[4/4] Bo qua dong goi (them -Package de tao installer)." -ForegroundColor DarkGray
}

Write-Host "`nHOAN TAT." -ForegroundColor Green
Write-Host "Chay thu:  .\publish\swico.exe --help" -ForegroundColor Gray
Write-Host ''
Write-Host 'Ve chu ky so: KHONG ky tay o day.' -ForegroundColor Yellow
Write-Host 'Viec ky do SignPath Foundation thuc hien tu dong trong .github/workflows/release.yml'
Write-Host 'khi day mot tag v*. Xem docs/SIGNING.md.'
