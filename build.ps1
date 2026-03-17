# Docker Manager Build Script
param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$SkipTests,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
$PublishDir = "$ProjectRoot\publish"
$AppProject = "$ProjectRoot\src\DockerManager.App\DockerManager.App.csproj"
$TestProject = "$ProjectRoot\tests\DockerManager.Core.Tests\DockerManager.Core.Tests.csproj"

Write-Host "=== Docker Manager Build ===" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host "Configuration: $Configuration"

# Clean
Write-Host "`n--- Pulizia ---" -ForegroundColor Yellow
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }

# Restore
Write-Host "`n--- Restore pacchetti ---" -ForegroundColor Yellow
dotnet restore "$ProjectRoot\DockerManager.sln"

# Build
Write-Host "`n--- Build ---" -ForegroundColor Yellow
dotnet build "$ProjectRoot\DockerManager.sln" -c $Configuration --no-restore

# Test
if (-not $SkipTests) {
    Write-Host "`n--- Test ---" -ForegroundColor Yellow
    dotnet test $TestProject -c $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Test falliti!" -ForegroundColor Red
        exit 1
    }
}

# Publish
Write-Host "`n--- Publish ---" -ForegroundColor Yellow
dotnet publish $AppProject -c $Configuration -r win-x64 --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:Version=$Version `
    -o $PublishDir

# Installer
if (-not $SkipInstaller) {
    Write-Host "`n--- Creazione Installer ---" -ForegroundColor Yellow
    $InnoSetup = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $InnoSetup) {
        & $InnoSetup "/DAppVersion=$Version" "$ProjectRoot\installer\setup.iss"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Errore creazione installer!" -ForegroundColor Red
            exit 1
        }
        Write-Host "Installer creato con successo!" -ForegroundColor Green
    } else {
        Write-Host "Inno Setup non trovato. Salto creazione installer." -ForegroundColor Yellow
    }
}

Write-Host "`n=== Build completata ===" -ForegroundColor Green
Write-Host "Output: $PublishDir"
