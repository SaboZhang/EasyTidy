using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using BackdropType = EasyTidy.Model.BackdropType;

namespace EasyTidy.Service;

public class ThemeSelectorService : IThemeSelectorService
{
    public ThemeSelectorService()
    {
        Window = App.MainWindow;
    }

    private void SetWindowSystemBackdrop(SystemBackdrop systemBackdrop)
    {
        App.MainWindow.SystemBackdrop = systemBackdrop;
    }

    public Window Window { get; set; }

    public ElementTheme Theme { get; set; } = ElementTheme.Default;

    public BackdropType BackdropType { get; set; } = BackdropType.Mica;

    private readonly ILocalSettingsService _localSettingsService;

    public event EventHandler<ElementTheme> ThemeChanged = (_, _) => { };

    public ThemeSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        BackdropType = await ConfigBackdrop();
        Theme = await LoadThemeFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetThemeAsync(ElementTheme theme)
    {
        Theme = theme;

        await SetRequestedThemeAsync();
        await SaveThemeInSettingsAsync(Theme);
    }

    public async Task SetRequestedThemeAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = Theme;

            TitleBarHelper.UpdateTitleBar(Theme);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementTheme> LoadThemeFromSettingsAsync()
    {
        var themeName = await _localSettingsService.ReadSettingsAsync();

        if (Enum.TryParse(themeName.ElementTheme.ToString(), out ElementTheme cacheTheme))
        {
            return cacheTheme;
        }

        return ElementTheme.Default;
    }

    private async Task SaveThemeInSettingsAsync(ElementTheme theme)
    {
        var settings = new CoreSettings
        {
            ElementTheme = theme,
            BackdropType = BackdropType
        };
        await _localSettingsService.SaveSettingsAsync(settings);
    }

    private async Task<BackdropType> ConfigBackdrop()
    {
        var themeName = await _localSettingsService.ReadSettingsAsync();
        if (Enum.TryParse(themeName.BackdropType.ToString(), out BackdropType backdropType))
        {
            if (themeName.IsFirstRun && backdropType == BackdropType.None)
            {
                await SetBackdropType(BackdropType.Mica);
                backdropType = BackdropType.Mica;
            }
            SetWindowSystemBackdrop(GetSystemBackdrop(backdropType));
            return backdropType;
        }

        return BackdropType.Mica;
    }

    public async Task SetBackdropType(BackdropType backdropType)
    {
        BackdropType = backdropType;
        var settings = await _localSettingsService.ReadSettingsAsync();
        if (settings.IsFirstRun)
        {
            settings.IsFirstRun = false;
        }
        settings.ElementTheme = Theme;
        settings.BackdropType = backdropType;
        SetWindowSystemBackdrop(GetSystemBackdrop(backdropType));
        await _localSettingsService.SaveSettingsAsync(settings);
    }

    public SystemBackdrop GetSystemBackdrop(BackdropType backdropType)
    {
        return backdropType switch
        {
            BackdropType.None => null,
            BackdropType.Mica => new MicaBackdrop() { Kind = MicaKind.Base },
            BackdropType.MicaAlt => new MicaBackdrop() { Kind = MicaKind.BaseAlt },
            BackdropType.DesktopAcrylic => new DesktopAcrylicBackdrop(),
            BackdropType.AcrylicBase => new AcrylicSystemBackdrop(DesktopAcrylicKind.Base),
            BackdropType.AcrylicThin => new AcrylicSystemBackdrop(DesktopAcrylicKind.Thin),
            _ => null,
        };
    }

    public BackdropType GetBackdropType()
    {
        return GetBackdropType(Window.SystemBackdrop);
    }

    public BackdropType GetBackdropType(SystemBackdrop systemBackdrop)
    {
        if (systemBackdrop is MicaBackdrop mica)
        {
            return mica.Kind == MicaKind.Base ? BackdropType.Mica : BackdropType.MicaAlt;
        }
        else if (systemBackdrop is AcrylicSystemBackdrop acrylic)
        {
            return acrylic.Kind == DesktopAcrylicKind.Base ? BackdropType.AcrylicBase : BackdropType.AcrylicThin;
        }
        else if (systemBackdrop is DesktopAcrylicBackdrop)
        {
            return BackdropType.DesktopAcrylic;
        }
        else
        {
            return BackdropType.None;
        }
    }

    public void SetRequestedTheme() => ThemeChanged(null, Theme);

    public async void ApplyTheme()
    {
        Theme = await LoadThemeFromSettingsAsync();
    }
}
