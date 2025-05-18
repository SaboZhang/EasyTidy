using EasyTidy.Model;
using System.Threading.Tasks;

namespace EasyTidy.Contracts.Service;

public interface ILocalSettingsService
{
    Task SaveSettingsAsync(CoreSettings settings);
    Task<CoreSettings?> ReadSettingsAsync();

    Task<T?> LoadSettingsAsync<T>(string? configFileName = null) where T : class;
    Task SaveSettingsAsync<T>(T settings, string? configFileName = null) where T : class;

}
