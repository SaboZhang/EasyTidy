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
    private IList<ContentOperatorEnum> contentOperators = Enum.GetValues(typeof(ContentOperatorEnum)).Cast<ContentOperatorEnum>().ToList();

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
            var value = string.IsNullOrWhiteSpace(dialog.CreateDateValue);
            if (dialog.HasErrors || (!dialog.IsSizeSelected && !dialog.IsCreateDateSelected && !dialog.IsEditDateSelected 
                && !dialog.IsVisitDateSelected && !dialog.IsContentSelected && string.IsNullOrWhiteSpace(dialog.SizeValue)
                && string.IsNullOrWhiteSpace(dialog.CreateDateValue) && string.IsNullOrWhiteSpace(dialog.EditDateValue)
                && string.IsNullOrWhiteSpace(dialog.VisitDateValue) && string.IsNullOrWhiteSpace(dialog.ContentValue)
                && string.IsNullOrWhiteSpace(dialog.IncludedFiles) && !dialog.IsArchiveSelected && !dialog.IsHiddenSelected
                && !dialog.IsReadOnlySelected && !dialog.IsSystemSelected && !dialog.IsTempSelected))
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = "至少需要设置一个过滤条件。",
                    ShowDateTime = false
                });
                args.Cancel = true;
                return;
            }
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
            Growl.Success(new GrowlInfo
            {
                Message = "添加成功",
                ShowDateTime = false
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"添加过滤器失败：{ex}");
            Growl.Error(new GrowlInfo
            {
                Message = "添加失败",
                ShowDateTime = false
            });
        }
    }

    [RelayCommand]
    private async Task OnDeleteTask(object dataContext)
    {
        IsActive = true;
        try
        {
            if (dataContext != null)
            {
                var task = dataContext as FilterTable;
                await using var db = new AppDbContext();
                var delete = await db.Filters.Where(x => x.Id == task.Id).FirstOrDefaultAsync();
                if (delete != null)
                {
                    db.Filters.Remove(delete);
                }
                await db.SaveChangesAsync();
                await OnPageLoaded();
                Growl.Success(new GrowlInfo
                {
                    Message = "删除成功",
                    ShowDateTime = false
                });
            }
        }
        catch (Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = "删除失败",
                ShowDateTime = false
            });
            Logger.Error($"FilterViewModel: OnDeleteTask 异常信息 {ex}");
            IsActive = false;
        }
        IsActive = false;
    }

    [RelayCommand]
    private async Task OnUpdateTask(object dataContext)
    {
        try
        {
            if (dataContext != null)
            {
                var dialog = new AddFilterContentDialog
                {
                    ViewModel = this,
                    Title = "修改",
                    PrimaryButtonText = "保存",
                    CloseButtonText = "取消",
                };

                var filter = dataContext as FilterTable;
                dialog.FilterName = filter.FilterName;
                dialog.IsSizeSelected = filter.IsSizeSelected;
                dialog.SizeOperator = filter.SizeOperator;
                dialog.SizeValue = filter.SizeValue;
                dialog.SizeUnit = filter.SizeUnit;
                dialog.IsCreateDateSelected = filter.IsCreateDateSelected;
                dialog.CreateDateOperator = filter.CreateDateOperator;
                dialog.CreateDateValue = filter.CreateDateValue;
                dialog.CreateDateUnit = filter.CreateDateUnit;
                dialog.IsEditDateSelected = filter.IsEditDateSelected;
                dialog.EditDateOperator = filter.EditDateOperator;
                dialog.EditDateValue = filter.EditDateValue;
                dialog.EditDateUnit = filter.EditDateUnit;
                dialog.IsVisitDateSelected = filter.IsVisitDateSelected;
                dialog.VisitDateOperator = filter.VisitDateOperator;
                dialog.VisitDateValue = filter.VisitDateValue;
                dialog.VisitDateUnit = filter.VisitDateUnit;
                dialog.IsArchiveSelected = filter.IsArchiveSelected;
                dialog.ArchiveValue = filter.ArchiveValue;
                dialog.IsHiddenSelected = filter.IsHiddenSelected;
                dialog.HiddenValue = filter.HiddenValue;
                dialog.IsReadOnlySelected = filter.IsReadOnlySelected;
                dialog.ReadOnlyValue = filter.ReadOnlyValue;
                dialog.IsSystemSelected = filter.IsSystemSelected;
                dialog.SystemValue = filter.SystemValue;
                dialog.IsTempSelected = filter.IsTempSelected;
                dialog.TempValue = filter.TempValue;
                dialog.IsIncludeSelected = filter.IsIncludeSelected;
                dialog.IncludedFiles = filter.IncludedFiles;
                dialog.ContentOperator = filter.ContentOperator;
                dialog.IsContentSelected = filter.IsContentSelected;
                dialog.ContentValue = filter.ContentValue;

                dialog.PrimaryButtonClick += async (s, e) =>
                {
                    if (dialog.HasErrors || (!dialog.IsSizeSelected && !dialog.IsCreateDateSelected && !dialog.IsEditDateSelected 
                        && !dialog.IsVisitDateSelected && !dialog.IsContentSelected && string.IsNullOrWhiteSpace(dialog.SizeValue)
                        && string.IsNullOrWhiteSpace(dialog.CreateDateValue) && string.IsNullOrWhiteSpace(dialog.EditDateValue)
                        && string.IsNullOrWhiteSpace(dialog.VisitDateValue) && string.IsNullOrWhiteSpace(dialog.ContentValue)
                        && string.IsNullOrWhiteSpace(dialog.IncludedFiles) && !dialog.IsArchiveSelected && !dialog.IsHiddenSelected
                        && !dialog.IsReadOnlySelected && !dialog.IsSystemSelected && !dialog.IsTempSelected))
                    {
                        Growl.Warning(new GrowlInfo
                        {
                            Message = "至少需要保留一个过滤条件。",
                            ShowDateTime = false
                        });
                        e.Cancel = true;
                        return;
                    }
                    await using var db = new AppDbContext();
                    var oldFilter = await db.Filters.Where(x => x.Id == filter.Id).FirstOrDefaultAsync();
                    oldFilter.FilterName = dialog.FilterName;
                    oldFilter.IsSizeSelected = dialog.IsSizeSelected;
                    oldFilter.SizeOperator = dialog.SizeOperator;
                    oldFilter.SizeValue = dialog.SizeValue;
                    oldFilter.SizeUnit = dialog.SizeUnit;
                    oldFilter.IsCreateDateSelected = dialog.IsCreateDateSelected;
                    oldFilter.CreateDateOperator = dialog.CreateDateOperator;
                    oldFilter.CreateDateValue = dialog.CreateDateValue;
                    oldFilter.CreateDateUnit = dialog.CreateDateUnit;
                    oldFilter.IsEditDateSelected = dialog.IsEditDateSelected;
                    oldFilter.EditDateOperator = dialog.EditDateOperator;
                    oldFilter.EditDateValue = dialog.EditDateValue;
                    oldFilter.EditDateUnit = dialog.EditDateUnit;
                    oldFilter.IsVisitDateSelected = dialog.IsVisitDateSelected;
                    oldFilter.VisitDateOperator = dialog.VisitDateOperator;
                    oldFilter.VisitDateValue = dialog.VisitDateValue;
                    oldFilter.VisitDateUnit = dialog.VisitDateUnit;
                    oldFilter.IsArchiveSelected = dialog.IsArchiveSelected;
                    oldFilter.ArchiveValue = dialog.ArchiveValue;
                    oldFilter.IsHiddenSelected = dialog.IsHiddenSelected;
                    oldFilter.HiddenValue = dialog.HiddenValue;
                    oldFilter.IsReadOnlySelected = dialog.IsReadOnlySelected;
                    oldFilter.ReadOnlyValue = dialog.ReadOnlyValue;
                    oldFilter.IsSystemSelected = dialog.IsSystemSelected;
                    oldFilter.SystemValue = dialog.SystemValue;
                    oldFilter.IsTempSelected = dialog.IsTempSelected;
                    oldFilter.TempValue = dialog.TempValue;
                    oldFilter.IsIncludeSelected = dialog.IsIncludeSelected;
                    oldFilter.IncludedFiles = dialog.IncludedFiles;
                    oldFilter.ContentOperator = dialog.ContentOperator;
                    oldFilter.IsContentSelected = dialog.IsContentSelected;
                    oldFilter.ContentValue = dialog.ContentValue;

                    await db.SaveChangesAsync();
                    await OnPageLoaded();
                    Growl.Success(new GrowlInfo
                    {
                        Message = "修改成功",
                        ShowDateTime = false
                    });
                };

                await dialog.ShowAsync();

            }

        }
        catch (Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = "修改失败",
                ShowDateTime = false
            });
            Logger.Error($"FileExplorerViewModel: OnUpdateTask 异常信息 {ex}");
        }

    }

}
