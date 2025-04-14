using System.Collections.Specialized;
using Microsoft.Windows.AppNotifications;

namespace EasyTidy.Contracts.Service;

public interface IAppNotificationService
{
    void Initialize();

    bool Show(string payload);

    bool Show(AppNotification appNotification);

    NameValueCollection ParseArguments(string arguments);

    void Unregister();
}
