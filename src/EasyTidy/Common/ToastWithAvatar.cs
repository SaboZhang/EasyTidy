using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications;

namespace EasyTidy.Common;
public class ToastWithAvatar
{
    private static ToastWithAvatar _Instance;

    public static ToastWithAvatar Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = new ToastWithAvatar();
            }
            return _Instance;
        }
        set { _Instance = value; }
    }

    public int ScenarioId { get; set; } = 1;
    public string ScenarioName { get; set; } = "系统提示";

    public string Description { get; set; } = "";

    public void NotificationReceived(AppNotificationActivatedEventArgs notificationActivatedEventArgs)
    {
        var notification = NotificationHelper.GetNotificationForWithAvatar(ScenarioName, notificationActivatedEventArgs);
    }

    public bool SendToast()
    {
        return SendToastWithAvatar(ScenarioId, ScenarioName, Description);
    }

    public static bool SendToastWithAvatar(int scenarioId, string scenarioName, string message)
    {
        string text = NotificationHelper.MakeScenarioIdToken(scenarioId);
        return ScenarioHelper.SendToast(new string("<toast launch = \"action=ToastClick&amp;" + text + "\"><visual><binding template = \"ToastGeneric\"><image placement = \"appLogoOverride\" hint-crop=\"circle\" src = \"" + PathHelper.GetFullPathToAsset("Logo.png") + "\"/><text>" + scenarioName + "</text><text>" + message + "</text></binding></visual></toast>"));
    }
}
