using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Contracts.Service;

public interface ILocalSettingsService
{
    Task<T?> LoadSettingsAsync<T>() where T : class;

    Task SaveSettingsAsync<T>(T settings) where T : class;

    Task SaveSettingsAsync(CoreSettings settings);
    Task<CoreSettings?> ReadSettingsAsync();

}
