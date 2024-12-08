using EasyTidy.Model;
using System.Threading.Tasks;

namespace EasyTidy.Contracts.Service;

public interface ILocalSettingsService
{
    Task<T?> LoadSettingsAsync<T>() where T : class;

    Task SaveSettingsAsync<T>(T settings) where T : class;

    Task SaveSettingsAsync(CoreSettings settings);
    Task<CoreSettings?> ReadSettingsAsync();

}
