using CommunityToolkit.WinUI;
using EasyTidy.Common.Model;
using EasyTidy.Contracts.Service;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using BackdropType = EasyTidy.Model.BackdropType;

namespace EasyTidy.ViewModels;
public partial class ThemeSettingViewModel : ObservableObject
{

    private readonly IThemeSelectorService _themeSelectorService;

    private int _themeIndex;

    private int _backDropIndex;

    [ObservableProperty]
    private ElementTheme _theme;

    [ObservableProperty]
    private BackdropType _backDrop;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public int ThemeIndex
    {
        get
        {
            return _themeIndex;
        }

        set
        {
            if (_themeIndex != value)
            {
                switch (value)
                {
                    case 0: Theme = ElementTheme.Light; break;
                    case 1: Theme = ElementTheme.Dark; break;
                    case 2: Theme = ElementTheme.Default; break;
                }

                _themeIndex = value;

                ApplyThemeOrBackdrop<ElementTheme>(Theme.ToString());

                OnPropertyChanged();
            }
        }
    }

    public int BackDropIndex
    {
        get
        {
            return _backDropIndex;
        }
        set 
        {
            if (_backDropIndex != value) 
            {
                switch (value) 
                {
                    case 0:
                        BackDrop = BackdropType.None;
                        break;
                    case 1:
                        BackDrop = BackdropType.Mica;
                        break;
                    case 2:
                        BackDrop = BackdropType.MicaAlt;
                        break;
                    case 3:
                        BackDrop = BackdropType.DesktopAcrylic;
                        break;
                    case 4:
                        BackDrop = BackdropType.AcrylicThin;
                        break;
                    case 5:
                        BackDrop = BackdropType.Transparent;
                        break;
                }

                _backDropIndex = value;

                ApplyThemeOrBackdrop<BackdropType>(BackDrop.ToString());

                OnPropertyChanged();
            }
        }
    }

    public ThemeSettingViewModel(IThemeSelectorService themeSelectorService)
    {
        _themeSelectorService = themeSelectorService;

        switch (_themeSelectorService.Theme)
        {
            case ElementTheme.Light:
                _themeIndex = 0;
                break;
            case ElementTheme.Dark:
                _themeIndex = 1;
                break;
            case ElementTheme.Default:
                _themeIndex = 2;
                break;
        }

        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new("Settings".GetLocalized(), typeof(SettingsViewModel).FullName!),
            new("Settings_Preferences_Header", typeof(ThemeSettingViewModel).FullName!),
        };
    }

    public SystemBackdrop GetSystemBackdrop(BackdropType backdropType)
    {
        switch (backdropType)
        {
            case BackdropType.None:
                return null;
            case BackdropType.Mica:
                return new MicaSystemBackdrop(MicaKind.Base);
            case BackdropType.MicaAlt:
                return new MicaSystemBackdrop(MicaKind.BaseAlt);
            case BackdropType.DesktopAcrylic:
                return new DesktopAcrylicBackdrop();
            case BackdropType.AcrylicBase:
                return new AcrylicSystemBackdrop(DesktopAcrylicKind.Base);
            case BackdropType.AcrylicThin:
                return new AcrylicSystemBackdrop(DesktopAcrylicKind.Thin);
            case BackdropType.Transparent:
                return new TransparentBackdrop();
            default:
                return null;
        }
    }

    private void ApplyThemeOrBackdrop<TEnum>(string text) where TEnum : struct
    {
        if (Enum.TryParse(text, out TEnum result) && Enum.IsDefined(typeof(TEnum), result))
        {
            if (result is BackdropType backdrop)
            {
                _themeSelectorService.SetBackdropType(backdrop);
            }
            else if (result is ElementTheme theme)
            {
                _themeSelectorService.SetThemeAsync(theme);
            }
        }
    }

}
