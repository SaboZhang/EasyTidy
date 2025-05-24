using EasyTidy.Model;
using Nucs.JsonSettings;
using Nucs.JsonSettings.Autosave;
using Nucs.JsonSettings.Fluent;
using Nucs.JsonSettings.Modulation;
using Nucs.JsonSettings.Modulation.Recovery;
using System.Security;

namespace EasyTidy.Common;

public static partial class AppHelper
{
    public static AppConfig Settings = JsonSettings.Configure<AppConfig>()
                               .WithRecovery(RecoveryAction.RenameAndLoadDefault)
                               .WithVersioning(VersioningResultAction.DoNothing)
                               .LoadNow()
                               .EnableAutosave();

    /// <summary>
    ///     初始化自启动
    /// </summary>
    /// <param name="isStartup"></param>
    private static void StartupOperate(bool isStartup)
    {
        if (isStartup)
        {
            if (!ShortcutUtil.IsStartup())
                ShortcutUtil.SetStartup();
        }
        else
        {
            ShortcutUtil.UnSetStartup();
        }
    }

    public static void UpdateCurConfig(GeneralSettingViewModel viewModel)
    {

        Settings.GeneralConfig.Minimize = viewModel.Minimize;
        Settings.GeneralConfig.AutomaticRepair = viewModel.AutomaticRepair;
        Settings.GeneralConfig.FileInUse = viewModel.FileInUse;
        Settings.GeneralConfig.SubFolder = viewModel.SubFolder;
        Settings.GeneralConfig.IsStartup = viewModel.IsStartup;
        Settings.GeneralConfig.IsStartupCheck = viewModel.IsStartupCheck;
        Settings.GeneralConfig.IsUseProxy = viewModel.IsUseProxy;
        Settings.GeneralConfig.ProxyAddress = viewModel.ProxyAddress;
        Settings.GeneralConfig.FileOperationType = viewModel.OperationType;
        Settings.GeneralConfig.EmptyFiles = viewModel.EmptyFiles;
        Settings.GeneralConfig.HiddenFiles = viewModel.HiddenFiles;
        Settings.BackupConfig.AutoBackup = viewModel.AutoBackup;
        Settings.GeneralConfig.EnableMultiInstance = viewModel.EnableMultiInstance;
        Settings.UploadPrefix = viewModel.WebDavPrefix;
        Settings.PreserveDirectoryStructure = viewModel.PreserveDirectoryStructure;
        StartupOperate(viewModel.IsStartup);
        Settings.GeneralConfig = Settings.GeneralConfig;
        Settings.BackupConfig = Settings.BackupConfig;

    }

    public static void UpdateCurConfig(AutomaticViewModel viewModel)
    {
        Settings.AutomaticConfig.IsFileChange = viewModel.IsFileChange;
        Settings.AutomaticConfig.IsStartupExecution = viewModel.IsStartupExecution;
        Settings.AutomaticConfig.RegularTaskRunning = viewModel.RegularTaskRunning;
        Settings.AutomaticConfig.OnScheduleExecution = viewModel.OnScheduleExecution;
        Settings.AutomaticConfig = Settings.AutomaticConfig;
    }

    public static string BuildToastXmlString(string title, string content, string? imageUri = null, Dictionary<string, string>? buttons = null)
    {
        // 转义避免非法字符
        string safeTitle = SecurityElement.Escape(title);
        string safeContent = SecurityElement.Escape(content);

        string xml = $@"
<toast>
  <visual>
    <binding template='ToastGeneric'>
      <text>{safeTitle}</text>
      <text>{safeContent}</text>"
      + (string.IsNullOrWhiteSpace(imageUri) ? "" : $@"
      <image placement='inline' src='{SecurityElement.Escape(imageUri)}' />")
      + @"
    </binding>
  </visual>";

        if (buttons != null && buttons.Count > 0)
        {
            xml += "<actions>";
            foreach (var btn in buttons)
            {
                xml += $@"<action content='{SecurityElement.Escape(btn.Key)}' arguments='{SecurityElement.Escape(btn.Value)}' />";
            }
            xml += "</actions>";
        }

        xml += "</toast>";
        return xml;
    }
    
    public static void DeleteUpdateArtifactsAtStartup()
    {
        // 在后台任务中延迟执行，不影响主线程
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(5));

            try
            {
                string baseDir = Constants.ExecutePath;

                // 删除 Update 文件夹
                string updateFolderPath = Path.Combine(baseDir, "Update");
                if (Directory.Exists(updateFolderPath))
                {
                    Directory.Delete(updateFolderPath, recursive: true);
                }

                // 删除 update.zip 文件
                string updateZipPath = Path.Combine(baseDir, "update.zip");
                if (File.Exists(updateZipPath))
                {
                    File.Delete(updateZipPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("更新文件清理失败: " + ex.Message);
            }
        });
    }

}

