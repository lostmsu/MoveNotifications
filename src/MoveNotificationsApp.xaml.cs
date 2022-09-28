namespace MoveNotifications;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

using LostTech.Stack.WindowManagement;

using Microsoft.Win32;

using PInvoke;

using Windows.UI.Notifications;

using Win32Exception = System.ComponentModel.Win32Exception;

public partial class MoveNotificationsApp : Application {
    readonly Win32WindowFactory windowFactory = new() { SuppressSystemMargin = false };
    volatile bool targetScreenPresent = false;
    static readonly System.Drawing.Point NotificationsPosition = new(-1428, -400);

    protected override async void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        SystemEvents.SessionSwitch += this.SystemEventsOnDisplaySettingsChanged;
        SystemEvents.DisplaySettingsChanged += this.SystemEventsOnDisplaySettingsChanged;
        this.SystemEventsOnDisplaySettingsChanged(null, EventArgs.Empty);

        while (true) {
            await Task.Delay(millisecondsDelay: 1000);

            IntPtr notificationHandle = User32.FindWindow("Windows.UI.Core.CoreWindow", "New notification");
            if (notificationHandle == IntPtr.Zero) continue;
            var notification = this.windowFactory.Create(notificationHandle);

            IntPtr actionCenterHandle = User32.FindWindow("Windows.UI.Core.CoreWindow", "Action center");
            if (actionCenterHandle == IntPtr.Zero) continue;
            var actionCenter = this.windowFactory.Create(actionCenterHandle);

            try {
                while (true) {
                    await Task.Delay(5);
                    if (!this.targetScreenPresent) continue;

                    if (!MoveNotificationWindow(notification)) break;
                    if (!MoveActionCenter(actionCenter)) break;
                }
            } catch (WindowNotFoundException) {
                break;
            }
        }
    }

    static bool MoveActionCenter(Win32Window actionCenter) {
        var bounds = actionCenter.Bounds;
        if (bounds.Height < 0.5)
            return true;
        if (!actionCenter.IsVisible)
            return true;
        var targetScreenBounds = System.Windows.Forms.Screen.GetBounds(NotificationsPosition);
        var targetBounds = bounds;
        targetBounds.X = targetScreenBounds.Right - targetBounds.Width - 2;
        targetBounds.Y = targetScreenBounds.Y + 2;
        return Move(actionCenter, targetBounds);
    }

    static bool MoveNotificationWindow(Win32Window notification) {
        var bounds = notification.Bounds;
        if (bounds.Height < 0.5)
            return true;
        if (!notification.IsVisible)
            return true;

        var targetBounds = bounds;
        targetBounds.X = NotificationsPosition.X;
        targetBounds.Y = NotificationsPosition.Y;
        return Move(notification, targetBounds);
    }

    static bool Move(Win32Window window, System.Drawing.RectangleF targetBounds) {
        if (!User32.MoveWindow(window.Handle, (int)targetBounds.X, (int)targetBounds.Y,
                                  (int)targetBounds.Width, (int)targetBounds.Height, true)) {
            var exception = new Win32Exception();
            Debug.WriteLine(exception);
            return false;
        }
        return true;
    }

    private void SystemEventsOnDisplaySettingsChanged(object? sender, EventArgs e) {
        var bounds = System.Windows.Forms.Screen.GetBounds(NotificationsPosition);
        this.targetScreenPresent = bounds.Contains(NotificationsPosition);
    }

    public enum WinApiErrorCode {
        ERROR_ACCESS_DENIED = 5,

        ERROR_INVALID_WINDOW_HANDLE = 1400,

        ERROR_INVALID_MONITOR_HANDLE = 1461,
    }
}
