using CommunityToolkit.WinUI.UI;
using EasyTidy.Model;
using EasyTidy.Util;
using EasyTidy.Views.ContentDialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;

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

    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public IThemeService themeService;

    [ObservableProperty]
    private IList<YesOrNo> yesOrNos = Enum.GetValues(typeof(YesOrNo)).Cast<YesOrNo>().ToList();

    [ObservableProperty]
    private IList<DateUnit> dateUnits = Enum.GetValues(typeof(DateUnit)).Cast<DateUnit>().ToList();

    [ObservableProperty]
    private IList<ComparisonResult> comparisonResults = Enum.GetValues(typeof(ComparisonResult)).Cast<ComparisonResult>().ToList();

    [ObservableProperty]
    private IList<SizeUnit> sizeUnits = Enum.GetValues(typeof(SizeUnit)).Cast<SizeUnit>().ToList();

    [ObservableProperty]
    public ObservableCollection<FilterTable> _filtersList;

    [ObservableProperty]
    public AdvancedCollectionView _filtersListACV;

    [RelayCommand]
    private async Task OnPageLoaded()
    {
        IsActive = true;
        try
        {
            await Task.Run(() =>
            {
                dispatcherQueue.TryEnqueue(async () =>
                {
                    await using var db = new AppDbContext();
                    // 查询所有过滤器
                    var list = await db.Filters.ToListAsync();
                    foreach (var item in list)
                    {
                        item.CharacterValue = item.BuildCharacterValue();
                        item.AttributeValue = item.BuildAttributeValue();
                        item.OtherValue = item.BuildOtherValue();
                    }
                    FiltersList = new(list);
                    FiltersListACV = new AdvancedCollectionView(FiltersList, true);
                    FiltersListACV.SortDescriptions.Add(new SortDescription("Id", SortDirection.Ascending));
                });
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"FilterViewModel 初始化加载失败：{ex}");
            IsActive = false;
        }
        IsActive = false;
    }

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
            await OnPageLoaded();
        }
        catch (Exception ex) 
        { 
            Logger.Error($"添加过滤器失败：{ex}");
        }
    }

}
