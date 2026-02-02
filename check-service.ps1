#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Checks the status of the Hikvision Sync Service.

.EXAMPLE
    .\check-service.ps1
#>

$ServiceName = "HikvisionSyncService"
$InstallPath = "C:\Services\HikvisionSync\publish"

Write-Host "=== Hikvision Sync Service Status ===" -ForegroundColor Cyan
Write-Host ""

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "Service is NOT installed." -ForegroundColor Yellow
    exit 1
}

Write-Host "Service Name:  $ServiceName"
Write-Host "Display Name:  $($service.DisplayName)"
Write-Host "Status:        $($service.Status)" -ForegroundColor $(if ($service.Status -eq "Running") { "Green" } else { "Yellow" })
Write-Host "Start Type:    $($service.StartType)"
Write-Host ""

if (Test-Path $InstallPath) {
    Write-Host "Install Path:  $InstallPath"
    $exe = Join-Path $InstallPath "hikvision_integration.exe"
    if (Test-Path $exe) {
        $fileInfo = Get-Item $exe
        Write-Host "Executable:    $exe"
        Write-Host "Last Modified: $($fileInfo.LastWriteTime)"
    }
} else {
    Write-Host "Install path not found: $InstallPath" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Recent Application Logs (last 10 .NET Runtime entries):" -ForegroundColor Gray
try {
    Get-EventLog -LogName Application -Source ".NET Runtime" -Newest 10 -ErrorAction SilentlyContinue |
        ForEach-Object { Write-Host "  [$($_.TimeGenerated)] $($_.Message.Substring(0, [Math]::Min(100, $_.Message.Length)))..." }
} catch {
    Write-Host "  (Unable to read Event Log - run as Administrator)" -ForegroundColor Gray
}
