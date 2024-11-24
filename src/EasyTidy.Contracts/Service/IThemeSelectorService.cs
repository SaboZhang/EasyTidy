using EasyTidy.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Contracts.Service;

public interface IThemeSelectorService
{
    Window Window { get; set; }

    ElementTheme Theme
    {
        get;
    }

    Task InitializeAsync();

    Task SetThemeAsync(ElementTheme theme);

    Task SetRequestedThemeAsync();

    Task SetBackdropType(BackdropType backdropType);

    BackdropType GetBackdropType(SystemBackdrop systemBackdrop);

    BackdropType GetBackdropType();
}
