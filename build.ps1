<#
    build.ps1 - Build va dong goi tsudev System Audit
    Chay tren may Windows co .NET 8 SDK va ket noi internet (de tai NuGet).
#>
param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host '=== 1/3 Chay unit test (Core - khong can Windows API) ===' -ForegroundColor Cyan
if (-not $SkipTests) {
    dotnet run --project tests/unittests -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Unit test THAT BAI - dung build.' }
} else {
    Write-Host 'Da bo qua test (-SkipTests).' -ForegroundColor DarkGray
}

Write-Host "`n=== 2/3 Build toan bo solution ===" -ForegroundColor Cyan
dotnet build src/Tsudev.Audit.Cli -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'Build THAT BAI.' }

Write-Host "`n=== 3/3 Publish thanh 1 file .exe duy nhat ===" -ForegroundColor Cyan
dotnet publish src/Tsudev.Audit.Cli -c $Configuration -r win-x64 -o publish
if ($LASTEXITCODE -ne 0) { throw 'Publish THAT BAI.' }

$exe = Join-Path $PSScriptRoot 'publish/tsudev-audit.exe'
if (Test-Path $exe) {
    $size = [math]::Round((Get-Item $exe).Length / 1MB, 1)
    Write-Host "`nHOAN TAT: $exe ($size MB)" -ForegroundColor Green
    Write-Host 'Chay thu:  .\publish\tsudev-audit.exe --help' -ForegroundColor Gray
    Write-Host ''
    Write-Host 'BUOC TIEP THEO (khi da co chung chi ky so):' -ForegroundColor Yellow
    Write-Host '  signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a publish\tsudev-audit.exe'
} else {
    throw "Khong tim thay file exe sau khi publish."
}
