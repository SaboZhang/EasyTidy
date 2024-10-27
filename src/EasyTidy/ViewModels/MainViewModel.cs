using EasyTidy.Common.Database;
using EasyTidy.Model;
using EasyTidy.Service;
using Microsoft.EntityFrameworkCore;
using WinRT;

namespace EasyTidy.ViewModels;
public partial class MainViewModel : ObservableObject, ITitleBarAutoSuggestBoxAware
{
    private readonly AppDbContext _dbContext;

    public IJsonNavigationViewService JsonNavigationViewService;
    public MainViewModel(IJsonNavigationViewService jsonNavigationViewService, IThemeService themeService)
    {
        JsonNavigationViewService = jsonNavigationViewService;
        themeService.Initialize(App.MainWindow, true, Constants.CommonAppConfigPath);
        themeService.ConfigBackdrop();
        themeService.ConfigElementTheme();
        _dbContext = App.GetService<AppDbContext>();
        OnStartupExecutionAsync().ConfigureAwait(false);
    }

    public void OnAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {

    }

    public void OnAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {

    }

    /// <summary>
    /// 启动时执行
    /// </summary>
    /// <returns></returns>
    private async Task OnStartupExecutionAsync()
    {
        var list = _dbContext.Automatic.Include(a => a.TaskOrchestrationList).Where(a => a.IsStartupExecution == true).ToList();
        foreach (var item in list) 
        {
            foreach(var task in item.TaskOrchestrationList)
            {
                // 执行操作
                await OperationHandler.ExecuteOperationAsync(Convert.ToInt32(task.OperationMode), "示例参数1");
            }
        }

    }
}
