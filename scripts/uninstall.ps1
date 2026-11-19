[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Write-Host "Uninstalling Vigil..." -ForegroundColor Yellow

# 1. Stop running process
Get-Process Vigil -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 600

# 2. Remove shortcuts
$startMenuShortcut = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Vigil.lnk"
Remove-Item -Path $startMenuShortcut -Force -ErrorAction SilentlyContinue

$desktopShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) "Vigil.lnk"
Remove-Item -Path $desktopShortcut -Force -ErrorAction SilentlyContinue

# 3. Remove from Startup
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "VigilGuard" -ErrorAction SilentlyContinue

# 4. Remove from Uninstall Registry
Remove-Item -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Vigil" -Recurse -Force -ErrorAction SilentlyContinue

# 5. Remove from PATH
$installDir = Join-Path $env:LOCALAPPDATA "Programs\Vigil"
$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($currentPath -like "*$installDir*") {
    $newPath = ($currentPath -split ';' | Where-Object { $_ -and $_ -ne $installDir }) -join ';'
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
}

# 6. Remove installation directory
Remove-Item -Path $installDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "✓ Vigil has been uninstalled successfully." -ForegroundColor Green
