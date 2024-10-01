using EasyTidy.Model;
using EasyTidy.Views.ContentDialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.ViewModels;

public partial class FileExplorerViewModel : ObservableRecipient
{
    public IThemeService themeService;

    public FileExplorerViewModel(IThemeService themeService)
    {
        this.themeService = themeService;
    }

    public FileExplorerViewModel(){}

    [ObservableProperty]
    private IList<OperationMode> operationModes = Enum.GetValues(typeof(OperationMode)).Cast<OperationMode>().ToList();

    [ObservableProperty]
    private OperationMode _selectedOperationMode;

    [RelayCommand]
    private async Task OnAddTaskClick(object sender)
    {
        var dialog = new AddTaskContentDialog
        {
            
            Title = "添加任务",
            PrimaryButtonText = "保存",
            CloseButtonText = "取消"
        };

        await dialog.ShowAsync();
    }
}

