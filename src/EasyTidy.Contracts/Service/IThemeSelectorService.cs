using EasyTidy.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;

namespace EasyTidy.Contracts.Service;

public interface IThemeSelectorService
{
    public event EventHandler<ElementTheme> ThemeChanged;

    void ApplyTheme();

    void SetRequestedTheme();

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

    BackdropType BackdropType { get; }
}
