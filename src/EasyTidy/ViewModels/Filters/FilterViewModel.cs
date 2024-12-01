using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI.Collections;
using EasyTidy.Common.Database;
using EasyTidy.Common.Extensions;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Views.ContentDialogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;

namespace EasyTidy.ViewModels;

public partial class FilterViewModel : ObservableRecipient
{
    private readonly AppDbContext _dbContext;

    private StackedNotificationsBehavior? _notificationQueue;

    [ObservableProperty]
    private IThemeSelectorService _themeSelectorService;

    public FilterViewModel(IThemeSelectorService themeSelectorService)
    {
        ThemeSelectorService = themeSelectorService;
        _dbContext = App.GetService<AppDbContext>();
    }

    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

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

    public void Initialize(StackedNotificationsBehavior notificationQueue)
    {
        _notificationQueue = notificationQueue;
    }

    public void Uninitialize()
    {
        _notificationQueue = null;
    }

    /// <summary>
    /// 初始化加载页面
    /// </summary>
    /// <returns></returns>
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
                    // 查询所有过滤器
                    var list = await _dbContext.Filters.ToListAsync();
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

    /// <summary>
    /// 添加
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnAddFilterClickedAsync()
    {
        var dialog = new AddFilterContentDialog
        {
            ViewModel = this,
            Title = "添加过滤器",
            PrimaryButtonText = "SaveText".GetLocalized(),
            CloseButtonText = "CancelText".GetLocalized(),
        };

        dialog.PrimaryButtonClick += OnAddFilterPrimaryButton;

        await dialog.ShowAsync();
    }


    /// <summary>
    /// 添加过滤器
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
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
            await _dbContext.Filters.AddAsync(new FilterTable
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
            await _dbContext.SaveChangesAsync();
            await OnPageLoaded();
            _notificationQueue.ShowWithWindowExtension("SaveSuccessfulText".GetLocalized(), InfoBarSeverity.Success);
            _ = ClearNotificationAfterDelay(3000);
        }
        catch (Exception ex)
        {
            Logger.Error($"添加过滤器失败：{ex}");
            _notificationQueue.ShowWithWindowExtension("SaveSuccessfulText".GetLocalized(), InfoBarSeverity.Error);
            _ = ClearNotificationAfterDelay(3000);
        }
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnDeleteFilter(object dataContext)
    {
        IsActive = true;
        try
        {
            if (dataContext != null)
            {
                var task = dataContext as FilterTable;
                var delete = await _dbContext.Filters.Where(x => x.Id == task.Id).FirstOrDefaultAsync();
                if (delete != null)
                {
                    _dbContext.Filters.Remove(delete);
                }
                await _dbContext.SaveChangesAsync();
                await OnPageLoaded();
                _notificationQueue.ShowWithWindowExtension("DeleteSuccessfulText".GetLocalized(), InfoBarSeverity.Success);
                _ = ClearNotificationAfterDelay(3000);
            }
        }
        catch (Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("DeleteFailedText".GetLocalized(), InfoBarSeverity.Error);
            _ = ClearNotificationAfterDelay(3000);
            Logger.Error($"FilterViewModel: OnDeleteTask 异常信息 {ex}");
            IsActive = false;
        }
        IsActive = false;
    }

    /// <summary>
    /// 修改
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    [RelayCommand]
    private async Task OnUpdateFilter(object dataContext)
    {
        try
        {
            if (dataContext != null)
            {
                var dialog = new AddFilterContentDialog
                {
                    ViewModel = this,
                    Title = "ModifyText".GetLocalized(),
                    PrimaryButtonText = "SaveText".GetLocalized(),
                    CloseButtonText = "CancelText".GetLocalized(),
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
                    var oldFilter = await _dbContext.Filters.Where(x => x.Id == filter.Id).FirstOrDefaultAsync();
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

                    await _dbContext.SaveChangesAsync();
                    await OnPageLoaded();
                    _notificationQueue.ShowWithWindowExtension("ModifySuccessfullyText".GetLocalized(), InfoBarSeverity.Success);
                    _ = ClearNotificationAfterDelay(3000);
                };

                await dialog.ShowAsync();

            }

        }
        catch (Exception ex)
        {
            _notificationQueue.ShowWithWindowExtension("ModificationFailedText".GetLocalized(), InfoBarSeverity.Error);
            _ = ClearNotificationAfterDelay(3000);
            Logger.Error($"FilterViewModel: OnUpdateTask 异常信息 {ex}");
        }

    }

    private async Task ClearNotificationAfterDelay(int delayMilliseconds)
    {
        await Task.Delay(delayMilliseconds);  // 延迟指定的毫秒数
        _notificationQueue?.Clear();  // 清除通知
    }

}
