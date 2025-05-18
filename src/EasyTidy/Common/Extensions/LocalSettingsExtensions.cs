using EasyTidy.Contracts.Service;
using EasyTidy.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Common.Extensions;

public static class LocalSettingsExtensions
{
    public static Task SaveSettingsExtAsync<T>(this ILocalSettingsService service, T settings) 
        where T : class
    {
        var fileName = LocalSettingsOptions.GetFileName<T>();
        return service.SaveSettingsAsync(settings, fileName);
    }

    public static Task<T?> LoadSettingsExtAsync<T>(this ILocalSettingsService service)
        where T : class
    {
        var fileName = LocalSettingsOptions.GetFileName<T>();
        return service.LoadSettingsAsync<T>(fileName);
    }

}
