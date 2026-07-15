$processName = "NotificationGatekeeper"
$process = Get-Process -Name $processName -ErrorAction SilentlyContinue
$registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\com.automattic.beeper.desktop"

if (!(Test-Path $registryPath)) {
    New-Item -Path $registryPath -Force | Out-Null
}

if ($process) {
    Write-Host "Stopping Notification Gatekeeper..."
    Stop-Process -Name $processName -Force
    
    Write-Host "Turning Beeper native banners ON..."
    Set-ItemProperty -Path $registryPath -Name "ShowBanners" -Value 1 -Type DWord
    
    Write-Host "Gatekeeper is now OFF. (Note: It may take a moment for Windows to register the setting change)."
} else {
    Write-Host "Turning Beeper native banners OFF..."
    Set-ItemProperty -Path $registryPath -Name "ShowBanners" -Value 0 -Type DWord
    
    Write-Host "Starting Notification Gatekeeper..."
    $exePath = "c:\dev\Win notification time gate\NotificationGatekeeper\bin\Release\net8.0-windows10.0.19041.0\publish\NotificationGatekeeper.exe"
    
    if (Test-Path $exePath) {
        Start-Process -FilePath $exePath -WindowStyle Hidden
    } else {
        Write-Host "Could not find the Gatekeeper executable! Make sure it was compiled." -ForegroundColor Red
    }
    
    Write-Host "Gatekeeper is now ON."
}

Write-Host "Closing in 3 seconds..."
Start-Sleep -Seconds 3
