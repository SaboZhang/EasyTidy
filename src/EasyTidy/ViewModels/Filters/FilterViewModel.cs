using EasyTidy.Model;
using EasyTidy.Util;
using EasyTidy.Views.ContentDialogs;

namespace EasyTidy.ViewModels;

public partial class FilterViewModel : ObservableRecipient
{
    public FilterViewModel()
    {

    }

    public FilterViewModel(IThemeService themeService)
    {
        this.themeService = themeService;
    }

    public IThemeService themeService;

    [ObservableProperty]
    private IList<YesOrNo> yesOrNos = Enum.GetValues(typeof(YesOrNo)).Cast<YesOrNo>().ToList();

    [ObservableProperty]
    private IList<DateUnit> dateUnits = Enum.GetValues(typeof(DateUnit)).Cast<DateUnit>().ToList();

    [ObservableProperty]
    private IList<ComparisonResult> comparisonResults = Enum.GetValues(typeof(ComparisonResult)).Cast<ComparisonResult>().ToList();

    [ObservableProperty]
    private IList<SizeUnit> sizeUnits = Enum.GetValues(typeof(SizeUnit)).Cast<SizeUnit>().ToList();

    [RelayCommand]
    private async Task OnAddFilterClickedAsync()
    {
        var dialog = new AddFilterContentDialog
        {
            ViewModel = this,
            Title = "添加过滤器",
            PrimaryButtonText = "保存",
            CloseButtonText = "取消"
        };

        dialog.PrimaryButtonClick += OnAddFilterPrimaryButton;

        await dialog.ShowAsync();
    }

    private async void OnAddFilterPrimaryButton(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            var dialog = sender as AddFilterContentDialog;
            await using var db = new AppDbContext();
            await db.Filters.AddAsync(new FilterTable
            {
                FilterName = dialog.FilterName,
                IsSizeSelected = dialog.IsSizeSelected,
                SizeOperator = dialog.SizeOperator,
                SizeValue = dialog.SizeValue,
                SizeUnit = dialog.SizeUnit,
                IsCreateDateSelected = dialog.IsCreateDateSelected,
                CreateDateOperator = dialog.CreateDateOperator,
                CreateDateValue = dialog.CreateDateValue,
                CreateDateUnit = dialog.CreateDateUnit,
                IsEditDateSelected = dialog.IsEditDateSelected,
                EditDateOperator = dialog.EditDateOperator,
                EditDateValue = dialog.EditDateValue,
                EditDateUnit = dialog.EditDateUnit,
                IsVisitDateSelected = dialog.IsVisitDateSelected,
                VisitDateOperator = dialog.VisitDateOperator,
                VisitDateValue = dialog.VisitDateValue,
                VisitDateUnit = dialog.VisitDateUnit,
                IsArchiveSelected = dialog.IsArchiveSelected,
                ArchiveValue = dialog.ArchiveValue,
                IsHiddenSelected = dialog.IsHiddenSelected,
                HiddenValue = dialog.HiddenValue,
                IsReadOnlySelected = dialog.IsReadOnlySelected,
                ReadOnlyValue = dialog.ReadOnlyValue,
                IsSystemSelected = dialog.IsSystemSelected,
                SystemValue = dialog.SystemValue,
                IsTempSelected = dialog.IsTempSelected,
                TempValue = dialog.TempValue,
                IsIncludeSelected = dialog.IsIncludeSelected,
                IncludedFiles = dialog.IncludedFiles,
                ContentOperator = dialog.ContentOperator,
                IsContentSelected = dialog.IsContentSelected,
                ContentValue = dialog.ContentValue
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex) 
        { 
            Logger.Error($"添加过滤器失败：{ex}");
        }
    }

}
