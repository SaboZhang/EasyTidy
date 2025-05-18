using CommunityToolkit.WinUI;
using EasyTidy.Common.Model;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace EasyTidy.ViewModels;
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _selected;

    [ObservableProperty]
    private bool _isBackEnabled;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    [ObservableProperty]
    private ObservableCollection<SettingViewModel> _settingsList = new();

    public SettingsViewModel()
    {
        InitializeLanguages();
        var settings = new[]
        {
            new Setting("General", string.Empty, "Settings_General_Header".GetLocalized(), "Settings_General_Description".GetLocalized(), "ms-appx:///Assets/Fluent/general.png", false, false),
            new Setting("Hotkey", string.Empty, "Settings_Hotkey_Header".GetLocalized(), "Settings_Hotkey_Description".GetLocalized(), "ms-appx:///Assets/Fluent/keyboardManager.png", false, false),
            new Setting("AiSettings", string.Empty, "Settings_AI_Header".GetLocalized(), "Settings_AI_Description".GetLocalized(), "ms-appx:///Assets/Fluent/nlp2.png", false, false),
            new Setting("Theme", string.Empty, "ThemeSettingPage_Header".GetLocalized(), "ThemeSettingPage_Description".GetLocalized(), "ms-appx:///Assets/Fluent/theme.png", false, false),
            new Setting("AppUpdate", string.Empty, "AppUpdateSetting_Header".GetLocalized(), "AppUpdateSetting_Description".GetLocalized(), "ms-appx:///Assets/Fluent/update.png", false, false),
            new Setting("About", string.Empty, "AboutUsSettingPage_Header".GetLocalized(), "AboutUsSettingPage_Description".GetLocalized(), "ms-appx:///Assets/Fluent/info.png", false, false),
        };

        foreach (var setting in settings)
        {
            SettingsList.Add(new SettingViewModel(setting, this));
        }

        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new("Settings".GetLocalized(), typeof(SettingsViewModel).FullName!),
        };
    }

    private int _languagesIndex;

    private int _initLanguagesIndex;

    private bool _languageChanged;

    public ObservableCollection<LanguageModel> Languages { get; } = [];

    public void Navigate(string path)
    {
        var navigationService = App.GetService<INavigationService>();
        var segments = path.Split("/");
        switch (segments[0])
        {
            case "Theme":
                navigationService.NavigateTo(typeof(ThemeSettingViewModel).FullName!);
                return;
            case "General":
                navigationService.NavigateTo(typeof(GeneralSettingViewModel).FullName!);
                return;
            case "About":
                navigationService.NavigateTo(typeof(AboutUsSettingViewModel).FullName!);
                return;
            case "AppUpdate":
                navigationService.NavigateTo(typeof(AppUpdateSettingViewModel).FullName!);
                return;
            case "AiSettings":
                navigationService.NavigateTo(typeof(AiSettingsViewModel).FullName!);
                return;
            case "Hotkey":
                navigationService.NavigateTo(typeof(HotKeySettingViewModel).FullName!);
                return;
            default:
                return;
        }
    }

    /// <summary>
    /// 语言标签和语言ID
    /// </summary>
    private Dictionary<string, string> langTagsAndIds = new Dictionary<string, string>
    {
        { string.Empty, "Default_language" },
        { "ar-SA", "Arabic_Saudi_Arabia_Language" },
        { "cs-CZ", "Czech_Language" },
        { "de-DE", "German_Language" },
        { "en-US", "English_Language" },
        { "es-ES", "Spanish_Language" },
        { "fa-IR", "Persian_Farsi_Language" },
        { "fr-FR", "French_Language" },
        { "he-IL", "Hebrew_Israel_Language" },
        { "hu-HU", "Hungarian_Language" },
        { "it-IT", "Italian_Language" },
        { "ja-JP", "Japanese_Language" },
        { "ko-KR", "Korean_Language" },
        { "nl-NL", "Dutch_Language" },
        { "pl-PL", "Polish_Language" },
        { "pt-BR", "Portuguese_Brazil_Language" },
        { "pt-PT", "Portuguese_Portugal_Language" },
        { "ru-RU", "Russian_Language" },
        { "sv-SE", "Swedish_Language" },
        { "tr-TR", "Turkish_Language" },
        { "uk-UA", "Ukrainian_Language" },
        { "zh-CN", "Chinese_Simplified_Language" },
        { "zh-TW", "Chinese_Traditional_Language" },
    };

    /// <summary>
    /// 初始化
    /// </summary>
    private void InitializeLanguages()
    {
        var lang = Settings.Language ?? string.Empty;
        var selectedLanguageIndex = 0;

        foreach (var item in langTagsAndIds)
        {
            var language = new LanguageModel { Tag = item.Key, ResourceID = item.Value, Language = item.Value.GetLocalized() };
            var index = GetLanguageIndex(language.Language, item.Key == string.Empty);
            Languages.Insert(index, language);

            if (item.Key.Equals(lang, StringComparison.Ordinal))
            {
                selectedLanguageIndex = index;
            }
            else if (index <= selectedLanguageIndex)
            {
                selectedLanguageIndex++;
            }
        }

        _initLanguagesIndex = selectedLanguageIndex;
        LanguagesIndex = selectedLanguageIndex;
    }

    public int LanguagesIndex
    {
        get
        {
            return _languagesIndex;
        }

        set
        {
            if (_languagesIndex != value)
            {
                _languagesIndex = value;
                OnPropertyChanged(nameof(LanguagesIndex));
                NotifyLanguageChanged();
                if (_initLanguagesIndex != value)
                {
                    LanguageChanged = true;
                }
                else
                {
                    LanguageChanged = false;
                }
            }
        }
    }

    public bool LanguageChanged
    {
        get
        {
            return _languageChanged;
        }

        set
        {
            if (_languageChanged != value)
            {
                _languageChanged = value;
                OnPropertyChanged(nameof(LanguageChanged));
            }
        }
    }

    /// <summary>
    /// 获取语言index
    /// </summary>
    /// <param name="language"></param>
    /// <param name="isDefault"></param>
    /// <returns></returns>
    private int GetLanguageIndex(string language, bool isDefault)
    {
        if (Languages.Count == 0 || isDefault)
        {
            return 0;
        }

        for (var i = 1; i < Languages.Count; i++)
        {
            if (string.Compare(Languages[i].Language, language, StringComparison.CurrentCultureIgnoreCase) > 0)
            {
                return i;
            }
        }

        return Languages.Count;
    }

    /// <summary>
    /// 重启
    /// </summary>
    public void Restart()
    {
        Logger.Info("Restarting application");
        App._mutex.ReleaseMutex();
        var appPath = Environment.ProcessPath;
        Process.Start(appPath);
        Environment.Exit(0);
    }

    /// <summary>
    /// 通知语言更改
    /// </summary>
    private void NotifyLanguageChanged()
    {
        // 更改语言
        Settings.Language = Languages[_languagesIndex].Tag;
        Settings.Save();
    }
}