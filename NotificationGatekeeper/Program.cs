using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;
using CommunityToolkit.WinUI.Notifications;

namespace NotificationGatekeeper
{
    class Program
    {
        // --- CONFIGURATION ---
        // Add the names of the apps you want to gatekeep here. 
        // This must match the Display Name of the app in Windows Notifications.
        static readonly HashSet<string> TargetApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
        { 
            "Beeper" 
        };

        // How often (in minutes) to release the batch notification.
        static readonly int IntervalMinutes = 5;
        // ---------------------

        static int _interceptedCount = 0;
        static readonly object _lockObj = new object();
        static string _logFilePath;

        static async Task Main(string[] args)
        {
            SetupLogging();
            Log("Notification Gatekeeper started.");

            if (!Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener"))
            {
                Log("UserNotificationListener API is not supported on this system.");
                return;
            }

            UserNotificationListener listener = UserNotificationListener.Current;
            UserNotificationListenerAccessStatus accessStatus = await listener.RequestAccessAsync();

            if (accessStatus != UserNotificationListenerAccessStatus.Allowed)
            {
                Log($"Access to notifications denied. Status: {accessStatus}");
                new ToastContentBuilder()
                    .AddText("Notification Gatekeeper")
                    .AddText("Permission to read notifications was denied. Please allow it in Windows Settings.")
                    .Show();
                return;
            }

            // Subscribe to new notifications
            listener.NotificationChanged += Listener_NotificationChanged;
            Log("Successfully subscribed to notifications. Monitoring apps: " + string.Join(", ", TargetApps));

            // Run the continuous timer loop
            await RunTimerLoopAsync();
        }

        private static void SetupLogging()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appData, "NotificationGatekeeper");
            Directory.CreateDirectory(appFolder);
            _logFilePath = Path.Combine(appFolder, "notification_log.txt");
        }

        private static void Log(string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                Console.WriteLine(logEntry);
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch { /* Ignore logging errors to prevent crash */ }
        }

        private static void Listener_NotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
        {
            try
            {
                if (args.ChangeKind == UserNotificationChangedKind.Added)
                {
                    var notification = sender.GetNotification(args.UserNotificationId);
                    if (notification != null)
                    {
                        string appName = notification.AppInfo.DisplayInfo.DisplayName;

                        if (TargetApps.Contains(appName))
                        {
                            lock (_lockObj)
                            {
                                _interceptedCount++;
                            }

                            Log($"Intercepted notification from '{appName}'. Total batched: {_interceptedCount}");

                            // Delete the notification from Action Center
                            sender.RemoveNotification(args.UserNotificationId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error handling notification: {ex.Message}");
            }
        }

        private static async Task RunTimerLoopAsync()
        {
            while (true)
            {
                DateTime now = DateTime.Now;
                
                // Calculate next interval boundary
                int minuteMod = now.Minute % IntervalMinutes;
                int minutesToAdd = IntervalMinutes - minuteMod;
                
                DateTime nextTick = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(minutesToAdd);
                TimeSpan delay = nextTick - now;

                if (delay.TotalMilliseconds <= 0)
                {
                    delay = TimeSpan.FromMinutes(IntervalMinutes);
                }

                Log($"Next notification batch scheduled for {nextTick:HH:mm:ss}");

                await Task.Delay(delay);

                int currentCount;
                lock (_lockObj)
                {
                    currentCount = _interceptedCount;
                    _interceptedCount = 0; // Reset counter for the next batch
                }

                if (currentCount > 0)
                {
                    Log($"Dispatching batch notification for {currentCount} messages.");
                    
                    new ToastContentBuilder()
                        .AddText("Batched Notifications")
                        .AddText($"You received {currentCount} new messages from your gatekept apps.")
                        .Show();
                }
                else
                {
                    Log("No notifications intercepted in this interval.");
                }
            }
        }
    }
}
