using EasyTidy.Model;
using EasyTidy.Util;
using Nucs.JsonSettings;
using Nucs.JsonSettings.Autosave;
using Nucs.JsonSettings.Fluent;
using Nucs.JsonSettings.Modulation;
using Nucs.JsonSettings.Modulation.Recovery;

namespace EasyTidy.Common;
public static partial class AppHelper
{
    public static AppConfig Settings = JsonSettings.Configure<AppConfig>()
                               .WithRecovery(RecoveryAction.RenameAndLoadDefault)
                               .WithVersioning(VersioningResultAction.RenameAndLoadDefault)
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

    public static ConfigModel InitialConfig()
    {
        return new ConfigModel
        {
            Minimize = Settings?.GeneralConfig.Minimize ?? false,
            IrrelevantFiles = Settings?.GeneralConfig.IrrelevantFiles ?? false,
            FileInUse = Settings?.GeneralConfig.FileInUse ?? false,
            SubFolder = Settings?.GeneralConfig.SubFolder ?? false,
            IsStartup = Settings?.GeneralConfig.IsStartup ?? false,
            IsStartupCheck = Settings?.GeneralConfig.IsStartupCheck ?? false
        };
    }

    public static void UpdateCurConfig(GeneralViewModel viewModel)
    {

        Settings.GeneralConfig.Minimize = viewModel.Minimize;
        Settings.GeneralConfig.IrrelevantFiles = viewModel.IrrelevantFiles;
        Settings.GeneralConfig.FileInUse = viewModel.FileInUse;
        Settings.GeneralConfig.SubFolder = viewModel.SubFolder;
        Settings.GeneralConfig.IsStartup = viewModel.IsStartup;
        Settings.GeneralConfig.IsStartupCheck = viewModel.IsStartupCheck;
        StartupOperate(viewModel.IsStartup);
        Settings.GeneralConfig = Settings.GeneralConfig;

    }
}

