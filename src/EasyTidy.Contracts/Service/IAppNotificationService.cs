using Microsoft.Windows.AppNotifications;
using System.Collections.Specialized;

namespace EasyTidy.Contracts.Service;

public interface IAppNotificationService
{
    void Initialize();

    bool Show(string payload);

    bool Show(AppNotification appNotification);

    NameValueCollection ParseArguments(string arguments);

    void Unregister();
}
