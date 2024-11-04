using CommunityToolkit.WinUI;
using EasyTidy.Model;
using Microsoft.UI.Xaml.Media.Animation;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace EasyTidy.ViewModels;
public partial class SettingsViewModel : ObservableObject
{
    public IJsonNavigationViewService JsonNavigationViewService;
    public SettingsViewModel(IJsonNavigationViewService jsonNavigationViewService)
    {
        JsonNavigationViewService = jsonNavigationViewService;
        InitializeLanguages();
    }

    private int _languagesIndex;

    private int _initLanguagesIndex;

    private bool _languageChanged;

    public ObservableCollection<LanguageModel> Languages { get; } = [];

    [RelayCommand]
    private void GoToSettingPage(object sender)
    {
        var item = sender as SettingsCard;
        if (item.Tag != null)
        {
            Type pageType = Application.Current.GetType().Assembly.GetType($"EasyTidy.Views.{item.Tag}");

            if (pageType != null)
            {
                SlideNavigationTransitionInfo entranceNavigation = new()
                {
                    Effect = SlideNavigationTransitionEffect.FromRight
                };
                JsonNavigationViewService.NavigateTo(pageType, item.Header, false, entranceNavigation);
            }
        }
    }

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

    public void Restart()
    {
        Logger.Info("Restarting application");
        App._mutex.ReleaseMutex();
        var appPath = Environment.ProcessPath;
        Process.Start(appPath);
        Environment.Exit(0);
    }

    private void NotifyLanguageChanged()
    {
        // 更改语言
        Settings.Language = Languages[_languagesIndex].Tag;
        Settings.Save();
    }
}