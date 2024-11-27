using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Util.SettingsInterface;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
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

    public BackdropType Backdrop { get; set; } = BackdropType.Mica;

    private readonly ILocalSettingsService _localSettingsService;

    public ThemeSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        Theme = await LoadThemeFromSettingsAsync();
        Backdrop = await ConfigBackdrop();
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
            ElementTheme = theme
        };
        await _localSettingsService.SaveSettingsAsync(settings);
    }

    private async Task<BackdropType> ConfigBackdrop()
    {
        var themeName = await _localSettingsService.ReadSettingsAsync();
        if (Enum.TryParse(themeName.BackdropType.ToString(), out BackdropType backdropType))
        {
            return backdropType;
        }

        return BackdropType.Mica;
    }

    public async Task SetBackdropType(BackdropType backdropType)
    {
        var systemBackdrop = GetSystemBackdropFromLocalConfig(backdropType, false);

        SetWindowSystemBackdrop(systemBackdrop);

        AppData.Config.BackdropType = backdropType;
        var settings = new CoreSettings { BackdropType = backdropType };
        await _localSettingsService.SaveSettingsAsync(settings);
    }

    public SystemBackdrop GetSystemBackdrop(BackdropType backdropType)
    {
        switch (backdropType)
        {
            case BackdropType.None:
                return null;
            case BackdropType.Mica:
                return new MicaBackdrop(){Kind = MicaKind.Base};
            case BackdropType.MicaAlt:
                return new MicaBackdrop(){Kind = MicaKind.BaseAlt};
            case BackdropType.DesktopAcrylic:
                return new DesktopAcrylicBackdrop();
            case BackdropType.AcrylicBase:
                return new Common.AcrylicSystemBackdrop(DesktopAcrylicKind.Base);
            case BackdropType.AcrylicThin:
                return new Common.AcrylicSystemBackdrop(DesktopAcrylicKind.Thin);
            case BackdropType.Transparent:
                return new TransparentBackdrop();
            default:
                return null;
        }
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
        else if (systemBackdrop is Common.AcrylicSystemBackdrop acrylic)
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

    private SystemBackdrop GetSystemBackdropFromLocalConfig(BackdropType backdropType, bool ForceBackdrop)
    {
        BackdropType currentBackdrop = backdropType;
        if (AppData.Config != null)
        {
            currentBackdrop = AppData.Config.BackdropType;
        }

        return ForceBackdrop ? GetSystemBackdrop(backdropType) : GetSystemBackdrop(currentBackdrop);
    }
}
