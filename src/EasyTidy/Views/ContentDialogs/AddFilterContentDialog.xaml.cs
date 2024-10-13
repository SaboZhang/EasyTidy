// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using EasyTidy.Model;

namespace EasyTidy.Views.ContentDialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AddFilterContentDialog : ContentDialog
{
    public FilterViewModel ViewModel { get; set; }

    public string FilterName { get; set; }
    public bool IsSizeSelected { get; set; }
    public ComparisonResult SizeOperator { get; set; }
    public string? SizeValue { get; set; } = null;
    public SizeUnit SizeUnit { get; set; }

    public bool IsCreateDateSelected { get; set; }
    public ComparisonResult CreateDateOperator { get; set; }
    public string? CreateDateValue { get; set; } = null;
    public DateUnit CreateDateUnit { get; set; }

    public bool IsEditDateSelected { get; set; }
    public ComparisonResult EditDateOperator { get; set; }
    public string? EditDateValue { get; set; } = null;
    public DateUnit EditDateUnit { get; set; }

    public bool IsVisitDateSelected { get; set; }
    public ComparisonResult VisitDateOperator { get; set; }
    public string? VisitDateValue { get; set; } = null;
    public DateUnit VisitDateUnit { get; set; }

    public bool IsArchiveSelected { get; set; }
    public YesOrNo ArchiveValue { get; set; }

    public bool IsHiddenSelected { get; set; }
    public YesOrNo HiddenValue { get; set; }

    public bool IsReadOnlySelected { get; set; }
    public YesOrNo ReadOnlyValue { get; set; }

    public bool IsSystemSelected { get; set; }
    public YesOrNo SystemValue { get; set; }

    public bool IsTempSelected { get; set; }
    public YesOrNo TempValue { get; set; }

    public bool IsIncludeSelected { get; set; }
    public string IncludedFiles { get; set; }

    public bool IsContentSelected { get; set; }
    public string ContentOperator { get; set; }
    public string ContentValue { get; set; }

    public AddFilterContentDialog()
    {
        ViewModel = App.GetService<FilterViewModel>();
        this.InitializeComponent();
        XamlRoot = App.MainWindow.Content.XamlRoot;
        RequestedTheme = ViewModel.themeService.GetElementTheme();
    }
}
