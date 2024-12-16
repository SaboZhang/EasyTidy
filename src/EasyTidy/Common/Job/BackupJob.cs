using EasyTidy.Model;
using EasyTidy.Util;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.Common.Job;

public class BackupJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        Logger.Debug("BackupJob Execute");
        // 获取任务的 JobDetail
        var jobDetail = context.JobDetail;

        // 获取任务的 Key（包含名称和组名）
        var jobKey = jobDetail.Key;

        // 获取任务的名称和组名
        string jobName = jobKey.Name;
        string groupName = jobKey.Group;
        var localPath = context.MergedJobDataMap.GetString("LocalPath");
        Logger.Debug($"BackupJob Execute: {jobName} {groupName}");
        if (Enum.TryParse(groupName, true, out BackupType type)) // 忽略大小写
        {
            if (type == BackupType.Local)
            {
                await LocalBackupAsync(localPath);
            }
            if (type == BackupType.WebDav)
            {
                await WebDavBackupAsync();
            }
        }
    }

    private static async Task WebDavBackupAsync()
    {
        var pass = CryptoUtil.DesDecrypt(Settings.WebDavPassword);
        WebDavClient webDavClient = new(Settings.WebDavUrl, Settings.WebDavUser, pass);
        var zipFilePath = Path.Combine(Constants.CnfPath, $"EasyTidy_backup_{DateTime.Now:yyyyMMddHHmmss}.zip");
        ZipUtil.CompressFile(Constants.CnfPath, zipFilePath);
        var result = await webDavClient.UploadFileAsync(Settings.WebDavUrl + "/EasyTidy", zipFilePath);
        if(result)
        {
            Logger.Info("WebDavBackup: Upload Success");
        }
        else
        {
            Logger.Warn("WebDavBackup: Upload Failed");
        }
    }

    private static async Task LocalBackupAsync(string floderPath)
    {
        var zipFilePath = Path.Combine(floderPath, $"EasyTidy_backup_{DateTime.Now:yyyyMMddHHmmss}.zip");
        ZipUtil.CompressFile(Constants.CnfPath, zipFilePath);
        Logger.Info($"LocalBackup: {zipFilePath}");
        await Task.CompletedTask;
    }
}
