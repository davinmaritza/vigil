[CmdletBinding()]
param(
    [string]$SourceExe = "$PSScriptRoot\..\dist\Vigil.exe",
    [switch]$NoLaunch
)

$ErrorActionPreference = "Stop"

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  Installing Vigil (Windows Clamshell Guard)    " -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# 1. Resolve target directory in %LOCALAPPDATA%\Programs\Vigil
$installDir = Join-Path $env:LOCALAPPDATA "Programs\Vigil"
$targetExe = Join-Path $installDir "Vigil.exe"
$targetIcon = Join-Path $installDir "icon.ico"

# 2. Stop any running Vigil processes cleanly
Write-Host "Stopping any running instances of Vigil..." -ForegroundColor Yellow
Get-Process Vigil -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800

# 3. Create install directory
Write-Host "Creating installation directory: $installDir" -ForegroundColor Gray
New-Item -ItemType Directory -Path $installDir -Force | Out-Null

# 4. Copy executable and assets
Write-Host "Copying Vigil binary and assets..." -ForegroundColor Gray
if (-not (Test-Path $SourceExe)) {
    throw "Source executable not found at: $SourceExe"
}
Copy-Item -Path $SourceExe -Destination $targetExe -Force

$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$iconSource = Join-Path $repoRoot "src\Vigil\icon.ico"
if (Test-Path $iconSource) {
    Copy-Item -Path $iconSource -Destination $targetIcon -Force
}

$readmeSource = Join-Path $repoRoot "README.md"
if (Test-Path $readmeSource) {
    Copy-Item -Path $readmeSource -Destination (Join-Path $installDir "README.md") -Force
}
$licenseSource = Join-Path $repoRoot "LICENSE"
if (Test-Path $licenseSource) {
    Copy-Item -Path $licenseSource -Destination (Join-Path $installDir "LICENSE") -Force
}

# 5. Create Shortcuts
$wsh = New-Object -ComObject WScript.Shell

# Start Menu Shortcut
$startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
$startMenuShortcut = Join-Path $startMenuDir "Vigil.lnk"
Write-Host "Creating Start Menu shortcut..." -ForegroundColor Gray
$sShortcut = $wsh.CreateShortcut($startMenuShortcut)
$sShortcut.TargetPath = $targetExe
$sShortcut.WorkingDirectory = $installDir
$sShortcut.Description = "Vigil - Windows Clamshell & Cooling Guard"
if (Test-Path $targetIcon) {
    $sShortcut.IconLocation = "$targetIcon,0"
}
$sShortcut.Save()

# Desktop Shortcut
$desktopDir = [Environment]::GetFolderPath('Desktop')
$desktopShortcut = Join-Path $desktopDir "Vigil.lnk"
Write-Host "Creating Desktop shortcut..." -ForegroundColor Gray
$dShortcut = $wsh.CreateShortcut($desktopShortcut)
$dShortcut.TargetPath = $targetExe
$dShortcut.WorkingDirectory = $installDir
$dShortcut.Description = "Vigil - Windows Clamshell & Cooling Guard"
if (Test-Path $targetIcon) {
    $dShortcut.IconLocation = "$targetIcon,0"
}
$dShortcut.Save()

# 6. Add to User PATH
Write-Host "Adding Vigil to User PATH environment variable..." -ForegroundColor Gray
$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($currentPath -notlike "*$installDir*") {
    $newPath = "$installDir;$currentPath"
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
    $env:Path = "$installDir;$env:Path"
}

# 7. Register in Windows Run (Startup)
Write-Host "Configuring automatic launch on Windows startup..." -ForegroundColor Gray
$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
Set-ItemProperty -Path $runKey -Name "VigilGuard" -Value "`"$targetExe`""

# 8. Register in Windows Installed Apps (Uninstall Registry)
Write-Host "Registering in Windows Installed Apps..." -ForegroundColor Gray
$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Vigil"
if (-not (Test-Path $uninstallKey)) {
    New-Item -Path $uninstallKey -Force | Out-Null
}
Set-ItemProperty -Path $uninstallKey -Name "DisplayName" -Value "Vigil"
Set-ItemProperty -Path $uninstallKey -Name "DisplayVersion" -Value "1.0.2"
Set-ItemProperty -Path $uninstallKey -Name "Publisher" -Value "Davin Maritza"
Set-ItemProperty -Path $uninstallKey -Name "DisplayIcon" -Value "$targetIcon"
Set-ItemProperty -Path $uninstallKey -Name "InstallLocation" -Value "$installDir"
Set-ItemProperty -Path $uninstallKey -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$PSScriptRoot\uninstall.ps1`""

# 9. Launch application
if (-not $NoLaunch) {
    Write-Host "Launching Vigil..." -ForegroundColor Green
    [System.Diagnostics.Process]::Start($targetExe) | Out-Null
}

Write-Host "`n✓ Vigil successfully installed to $installDir" -ForegroundColor Green
Write-Host "✓ Start Menu & Desktop shortcuts created" -ForegroundColor Green
Write-Host "✓ Added to User PATH (type 'vigil' from any terminal)" -ForegroundColor Green
