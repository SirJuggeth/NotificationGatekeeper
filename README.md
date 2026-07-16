# Notification Gatekeeper

A lightweight Windows background application designed to intercept, batch, and deliver desktop notifications on a fixed time interval.

## Overview
Notification Gatekeeper taps into the native Windows `UserNotificationListener` API to monitor incoming notifications from specific apps (like Beeper). Instead of letting the app ping you continuously, it intercepts the notification, deletes the banner from the Windows Action Center, and keeps a running tally.

On a continuous, fixed clock interval (e.g., every 5 minutes at 12:00, 12:05, 12:10), the application will check if any messages were intercepted. If the count is greater than zero, it will deliver a single summary Toast Notification (e.g., "Beeper: 11 new messages received.") and reset the counter.

## Features
*   **Clock-Aligned Batching:** Notifications are delivered exactly on the minute marks, not randomly based on when the first message arrived.
*   **Native Windows Integration:** Built using C# and Windows App SDK, allowing it to hook directly into the system Action Center.
*   **Automatic Cleanup:** Original notifications are deleted from the Action Center so you don't get double-pinged.
*   **Local Logging:** Every intercepted notification is logged to `%LocalAppData%\NotificationGatekeeper\notification_log.txt` for troubleshooting and auditing.
*   **Easy Toggle Script:** Includes a double-clickable PowerShell script to easily turn the background app on/off and automatically restore the target app's native Windows notification settings.

## Installation
Because this application intercepts system notifications, Windows requires it to be explicitly registered and granted permission.

1.  **Enable Developer Mode:** Go to `Windows Settings > Privacy & security > For developers` and turn Developer Mode **ON**.
2.  **Register the App:** Open PowerShell and run the following command to register the application on your system:
    ```powershell
    Add-AppxPackage -Register "C:\dev\Win notification time gate\NotificationGatekeeper\bin\Release\net8.0-windows10.0.19041.0\publish\AppxManifest.xml"
    ```
3.  **Silence Native Notifications:** Go to `Windows Settings > System > Notifications > Beeper` (or your target app). Turn **OFF** "Show notification banners" and "Play a sound", but leave **ON** "Show in notification center".
4.  **First Run:** Launch "Notification Gatekeeper" from your Start Menu. When prompted by Windows, **Allow** the app to access your notifications.

## Configuration
Currently, target apps and interval lengths are configured at the top of the `Program.cs` file. To modify them, you must edit the file and recompile the application using the .NET 8 SDK.
