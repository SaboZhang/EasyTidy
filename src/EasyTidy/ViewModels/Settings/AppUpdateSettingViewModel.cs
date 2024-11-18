using CommunityToolkit.WinUI;
using EasyTidy.Model;
using Windows.System;

namespace EasyTidy.ViewModels;
public partial class AppUpdateSettingViewModel : ObservableObject
{
    [ObservableProperty]
    public string currentVersion;

    [ObservableProperty]
    public string lastUpdateCheck;

    [ObservableProperty]
    public bool isUpdateAvailable;

    [ObservableProperty]
    public bool isLoading;

    [ObservableProperty]
    public bool isCheckButtonEnabled = true;

    [ObservableProperty]
    public string loadingStatus = "Status".GetLocalized();

    private string ChangeLog = string.Empty;

    public IThemeService themeService;

    public AppUpdateSettingViewModel()
    {
        CurrentVersion = string.Format("CurrentVersion".GetLocalized(), App.Current.AppVersion);
        LastUpdateCheck = Settings.LastUpdateCheck;
        themeService = App.GetService<IThemeService>();
    }

    /// <summary>
    /// 检查更新
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task CheckForUpdateAsync()
    {
        IsLoading = true;
        IsUpdateAvailable = false;
        IsCheckButtonEnabled = false;
        LoadingStatus = "CheckingForNewVersion".GetLocalized();
        if (NetworkHelper.IsNetworkAvailable())
        {
            try
            {
                //Todo: Fix UserName and Repo
                string username = "SaboZhang";
                string repo = "EasyTidy";
                LastUpdateCheck = DateTime.Now.ToShortDateString();
                Settings.LastUpdateCheck = DateTime.Now.ToShortDateString();
                var update = await UpdateHelper.CheckUpdateAsync(username, repo, new Version(App.Current.AppVersion));
                if (update.IsExistNewVersion)
                {
                    IsUpdateAvailable = true;
                    ChangeLog = update.Changelog;
                    LoadingStatus = string.Format("FoundANewVersion".GetLocalized(), update.TagName, update.CreatedAt, update.PublishedAt);
                }
                else
                {
                    LoadingStatus = "LatestVersion".GetLocalized();
                }
            }
            catch (Exception ex)
            {
                LoadingStatus = ex.Message;
                IsLoading = false;
                IsCheckButtonEnabled = true;
            }
        }
        else
        {
            LoadingStatus = "ErrorConnection".GetLocalized();
        }
        IsLoading = false;
        IsCheckButtonEnabled = true;
    }

    /// <summary>
    /// 启动时检查更新
    /// </summary>
    /// <returns></returns>
    public async Task CheckForNewVersionAsync()
    {
        if (NetworkHelper.IsNetworkAvailable() && (bool)Settings.GeneralConfig.IsStartupCheck)
        {
            try
            {
                //Todo: Fix UserName and Repo
                string username = "SaboZhang";
                string repo = "EasyTidy";
                LastUpdateCheck = DateTime.Now.ToShortDateString();
                Settings.LastUpdateCheck = DateTime.Now.ToShortDateString();
                var update = await UpdateHelper.CheckUpdateAsync(username, repo, new Version(App.Current.AppVersion));
                if (update.IsExistNewVersion)
                {
                    IsUpdateAvailable = true;
                    ChangeLog = update.Changelog;
                    LoadingStatus = string.Format("FoundANewVersion".GetLocalized(), update.TagName, update.CreatedAt, update.PublishedAt);
                    App.MainWindow.DispatcherQueue.TryEnqueue(async () =>
                    {
                        await ShowUpdateDialogAsync(update.Changelog, update.Assets[0].Url);
                    });
                }
                else
                {
                    Logger.Info($"已是新版本");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"检查更新失败：{ex.Message}");
            }
        }
        else
        {
            Logger.Info($"检查更新失败：未启用开机自动更新或者网络异常");
        }
    }

    private async Task ShowUpdateDialogAsync(string changelog, string downloadUrl)
    {
        var mainWindow = App.MainWindow;
        if (mainWindow == null)
        {
            Logger.Error("主窗口未初始化，无法显示更新对话框.");
            throw new InvalidOperationException("Main window is not initialized.");
        }

        // 创建 ContentDialog 来显示更新信息
        var updateDialog = new ContentDialog
        {
            Title = "ReleaseNotes".GetLocalized(),
            Content = new TextBlock
            {
                Text = $"Changelog:\n\n{changelog}",
                TextWrapping = TextWrapping.Wrap
            },
            CloseButtonText = "Close".GetLocalized(),
            PrimaryButtonText = "Download",

            XamlRoot = mainWindow.Content.XamlRoot,
            RequestedTheme = themeService.GetElementTheme()
        };

        // 下载按钮的点击事件
        updateDialog.PrimaryButtonClick += async (sender, args) =>
        {
            // 打开下载链接或实现下载逻辑
            try
            {
                Download(downloadUrl);
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "Error".GetLocalized(),
                    Content = $"Could not open download link. {ex.Message}",
                    CloseButtonText = "OK".GetLocalized()
                }.ShowAsync();
            }
        };

        // 显示对话框
        await updateDialog.ShowAsync();
    }

    private async void Download(string downloadUrl)
    {
        var httpClient = new HttpClient(new SocketsHttpHandler());

        try
        {
            if (!Directory.Exists(Constants.SaveDir)) Directory.CreateDirectory(Constants.SaveDir);

            using (var response =
                   await httpClient.GetAsync(new Uri(downloadUrl), HttpCompletionOption.ResponseHeadersRead))
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(Path.Combine(Constants.SaveDir, Constants.SaveName), FileMode.Create))
            {
                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                long totalDownloadedByte = 0;
                var buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);

                    totalDownloadedByte += bytesRead;
                    var process = Math.Round((double)totalDownloadedByte / totalBytes * 100, 2);
                }
            }

        }
        catch (Exception)
        {
            Logger.Error("下载失败");
        }
        finally
        {
            httpClient.Dispose();
        }
    }

    [RelayCommand]
    private async Task GoToUpdateAsync()
    {
        //Todo: Change Uri
        await Launcher.LaunchUriAsync(new Uri("https://github.com/SaboZhang/Organize/releases"));
    }

    [RelayCommand]
    private async Task GetReleaseNotesAsync()
    {
        ContentDialog dialog = new ContentDialog()
        {
            Title = "ReleaseNotes".GetLocalized(),
            CloseButtonText = "Close".GetLocalized(),
            Content = new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = ChangeLog,
                    Margin = new Thickness(10)
                },
                Margin = new Thickness(10)
            },
            Margin = new Thickness(10),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = App.MainWindow.Content.XamlRoot,
            RequestedTheme = themeService.GetElementTheme()
        };

        await dialog.ShowAsyncQueue();
    }
}
