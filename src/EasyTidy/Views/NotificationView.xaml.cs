﻿using H.NotifyIcon;
using H.NotifyIcon.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NotificationView
{
    public TaskbarIcon? TrayIcon { get; set; }

    public NotificationView()
    {
        InitializeComponent();
    }

    private void ShowNotificationButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedIcon = (Type.SelectedItem as RadioButton)?.Content;

        TrayIcon?.ShowNotification(
            title: TitleTextBox.Text,
            message: MessageTextBox.Text,
            icon: selectedIcon switch
            {
                "None" => NotificationIcon.None,
                "Information" => NotificationIcon.Info,
                "Warning" => NotificationIcon.Warning,
                "Error" => NotificationIcon.Error,
                _ => NotificationIcon.None,
            },
            customIconHandle: selectedIcon switch
            {
                "Custom" => TrayIcon.Icon?.Handle,
                _ => null,
            },
            //largeIcon: LargeIconCheckBox.IsChecked ?? false,
            sound: SoundCheckBox.IsChecked ?? true,
            respectQuietTime: true,
            realtime: false,
            timeout: null);
    }

    private void ClearNotificationsButton_Click(object sender, RoutedEventArgs e)
    {
        TrayIcon?.ClearNotifications();
    }
}
