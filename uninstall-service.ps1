#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Uninstalls the Hikvision Sync Service.

.DESCRIPTION
    Stops and removes the Windows Service. Optionally removes installed files.
    Run this script as Administrator.

.PARAMETER RemoveFiles
    If specified, also deletes the installed application files from C:\Services\HikvisionSync

.EXAMPLE
    .\uninstall-service.ps1
    .\uninstall-service.ps1 -RemoveFiles
#>

param(
    [switch]$RemoveFiles
)

$ErrorActionPreference = "Stop"
$ServiceName = "HikvisionSyncService"
$InstallPath = "C:\Services\HikvisionSync"

Write-Host "=== Hikvision Sync Service Uninstaller ===" -ForegroundColor Cyan
Write-Host ""

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "Service '$ServiceName' is not installed." -ForegroundColor Yellow
    if ($RemoveFiles -and (Test-Path $InstallPath)) {
        Write-Host "Removing files from $InstallPath..." -ForegroundColor Gray
        Remove-Item -Path $InstallPath -Recurse -Force
        Write-Host "Files removed." -ForegroundColor Green
    }
    exit 0
}

# Stop the service
Write-Host "Stopping service..." -ForegroundColor Gray
Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Delete the service
Write-Host "Removing service..." -ForegroundColor Gray
sc.exe delete $ServiceName

if ($LASTEXITCODE -eq 0) {
    Write-Host "Service removed successfully." -ForegroundColor Green
} else {
    Write-Host "Warning: Service may not have been fully removed." -ForegroundColor Yellow
}

# Optionally remove files
if ($RemoveFiles -and (Test-Path $InstallPath)) {
    Write-Host "Removing installed files from $InstallPath..." -ForegroundColor Gray
    Remove-Item -Path $InstallPath -Recurse -Force
    Write-Host "Files removed." -ForegroundColor Green
} elseif ($RemoveFiles) {
    Write-Host "Install path $InstallPath not found." -ForegroundColor Gray
}

Write-Host ""
Write-Host "Uninstall complete." -ForegroundColor Green
