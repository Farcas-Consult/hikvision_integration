#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs the Hikvision Sync Service as a Windows Service.

.DESCRIPTION
    Publishes the application, creates a Windows Service, and configures it to auto-start.
    Run this script as Administrator.

.EXAMPLE
    .\install-service.ps1
#>

$ErrorActionPreference = "Stop"
$ServiceName = "HikvisionSyncService"
$DisplayName = "Hikvision Sync Service"
$Description = "Syncs gym members from the gym system to Hikvision access control readers every 3 minutes."

# Default install path - change if needed
$InstallPath = "C:\Services\HikvisionSync"

Write-Host "=== Hikvision Sync Service Installer ===" -ForegroundColor Cyan
Write-Host ""

# Get script directory (project root)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

# Stop and remove existing service if present
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Stopping existing service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2

    Write-Host "Removing existing service..." -ForegroundColor Yellow
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}

# Create install directory
Write-Host "Creating install directory: $InstallPath" -ForegroundColor Gray
New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

# Publish the application
Write-Host "Publishing application for win-x64..." -ForegroundColor Gray
$publishPath = Join-Path $InstallPath "publish"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publishPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

# Copy configuration files
Write-Host "Copying configuration files..." -ForegroundColor Gray
Copy-Item -Path (Join-Path $ScriptDir "appsettings.json") -Destination $publishPath -Force
if (Test-Path (Join-Path $ScriptDir "appsettings.Development.json")) {
    Copy-Item -Path (Join-Path $ScriptDir "appsettings.Development.json") -Destination $publishPath -Force
}
if (Test-Path (Join-Path $ScriptDir ".env")) {
    Copy-Item -Path (Join-Path $ScriptDir ".env") -Destination $publishPath -Force
}

$exePath = Join-Path $publishPath "hikvision_integration.exe"

# Create the Windows Service
Write-Host "Creating Windows Service..." -ForegroundColor Gray
New-Service -Name $ServiceName `
    -BinaryPathName "`"$exePath`"" `
    -DisplayName $DisplayName `
    -Description $Description `
    -StartupType Automatic

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to create service!" -ForegroundColor Red
    exit 1
}

# Start the service
Write-Host "Starting service..." -ForegroundColor Gray
Start-Service -Name $ServiceName

# Verify
Start-Sleep -Seconds 2
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service -and $service.Status -eq "Running") {
    Write-Host ""
    Write-Host "=== Installation Complete ===" -ForegroundColor Green
    Write-Host "Service Name: $ServiceName"
    Write-Host "Install Path: $publishPath"
    Write-Host "Status: $($service.Status)"
    Write-Host ""
    Write-Host "The service will sync gym members to Hikvision every 3 minutes."
    Write-Host "View logs: Event Viewer -> Windows Logs -> Application"
    Write-Host "Or run: Get-EventLog -LogName Application -Source .NET Runtime -Newest 50"
} else {
    Write-Host ""
    Write-Host "Service was created but may not have started. Check Event Viewer for errors." -ForegroundColor Yellow
    Write-Host "Install Path: $publishPath"
}
