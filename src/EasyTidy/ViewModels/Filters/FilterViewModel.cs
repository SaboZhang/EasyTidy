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
        await dialog.ShowAsync();
    }

}
