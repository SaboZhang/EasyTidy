using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasyTidy.Common.Database;

public partial class BackupAndRestore
{
    public static void BackupDatabase(string sourcePath, string backupPath)
    {
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }

        try
        {
            using (var sourceConnection = new SQLiteConnection($"Data Source={sourcePath};Version=3;"))
            using (var backupConnection = new SQLiteConnection($"Data Source={backupPath};Version=3;"))
            {
                sourceConnection.Open();
                backupConnection.Open();

                // 使用 SQLiteConnection 的 BackupDatabase 方法执行备份
                sourceConnection.BackupDatabase(backupConnection, "main", "main", -1, null, 0);

                Logger.Info("Database backup completed successfully.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error occurred during backup: {ex.Message}");
        }
    }

    public static void RestoreDatabase(string backupPath, string targetPath)
    {
        if (!File.Exists(backupPath))
        {
            Logger.Error("Backup file does not exist.");
            return;
        }

        try
        {
            // 确保目标数据库文件存在，若不存在则创建
            if (!File.Exists(targetPath))
            {
                SQLiteConnection.CreateFile(targetPath);
            }

            using (var backupConnection = new SQLiteConnection($"Data Source={backupPath};Version=3;"))
            using (var targetConnection = new SQLiteConnection($"Data Source={targetPath};Version=3;"))
            {
                backupConnection.Open();
                targetConnection.Open();

                // 恢复过程
                targetConnection.BackupDatabase(backupConnection, "main", "main", -1, null, 0);

                Logger.Info("Database restoration completed successfully.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error occurred during restoration: {ex.Message}");
        }
    }
}
