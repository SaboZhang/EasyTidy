using EasyTidy.Views.ContentDialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.ViewModels;

public partial class AutomaticViewModel : ObservableRecipient
{
    public AutomaticViewModel(IThemeService themeService)
    {
        this.themeService = themeService;
    }

    public IThemeService themeService;

    [ObservableProperty]
    public bool isOpen = false;

    [RelayCommand]
    private async Task OnPlanExecution()
    {
        var dialog = new PlanExecutionContentDialog
        {
            ViewModel = this,
            Title = "时间表",
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
            ThemeService = themeService
        };

        await dialog.ShowAsync();
    }

    [RelayCommand]
    private async Task OnCustomConfig()
    {
        var dialog = new CustomConfigContentDialog
        {
            ViewModel = this,
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
        };

        await dialog.ShowAsync();
    }

    [RelayCommand]
    private void OnSelectTask()
    {
        IsOpen = true;
    }
}
