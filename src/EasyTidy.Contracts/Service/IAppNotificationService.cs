using System.Collections.Specialized;

namespace EasyTidy.Contracts.Service;

public interface IAppNotificationService
{
    void Initialize();

    bool Show(string payload);

    NameValueCollection ParseArguments(string arguments);

    void Unregister();
}
