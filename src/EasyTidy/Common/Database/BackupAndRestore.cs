using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasyTidy.Common.Database;

public partial class BackupAndRestore : DbContext
{
    public static void BackupDatabase(string sourcePath, string backupPath)
    {
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }

        using (var source = new SqliteConnection($"Data Source={sourcePath};"))
        {
            source.Open();

            // 如果没有事务活动，不执行 COMMIT
            using (var command = source.CreateCommand())
            {
                command.CommandText = "PRAGMA busy_timeout = 5000;"; // 防止锁竞争
                command.ExecuteNonQuery();
            }

            using (var destination = new SqliteConnection($"Data Source={backupPath};"))
            {
                destination.Open();
                source.BackupDatabase(destination);
                destination.Dispose();
            }
        }
    }

    // 静态 JsonSerializerOptions 实例
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true, // 美化输出
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 防止特殊字符转义
    };

    /// <summary>
    /// 导出数据库为 JSON 文件
    /// </summary>
    /// <param name="outputPath"></param>
    /// <returns></returns>
    public async Task ExportDatabaseToJsonAsync(string outputPath)
    {
        // 创建一个字典存储所有表的数据
        var databaseData = new Dictionary<string, object>();

        // 通过反射获取 DbSet 属性
        var dbSets = this.GetType()
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

        foreach (var dbSetProperty in dbSets)
        {
            var tableName = dbSetProperty.Name; // 获取表名（属性名）
            var dbSetType = dbSetProperty.PropertyType.GetGenericArguments()[0]; // 获取 DbSet<> 的泛型类型
            var dbSet = dbSetProperty.GetValue(this); // 获取 DbSet 实例

            // 使用 EF Core 加载表数据
            var data = await ((IQueryable<object>)dbSet).ToListAsync();

            // 添加到字典
            databaseData[tableName] = data;
        }

        var json = JsonSerializer.Serialize(databaseData, JsonOptions);

        File.WriteAllText(outputPath, json);

        Logger.Info($"数据库已成功导出到: {outputPath}");
    }

    /// <summary>
    /// 从 JSON 文件导入数据库
    /// </summary>
    /// <param name="inputPath"></param>
    /// <returns></returns>
    public async Task ImportDatabaseFromJsonAsync(string inputPath)
    {
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"文件不存在: {inputPath}");
            return;
        }

        try
        {
            // 读取 JSON 文件内容
            var json = await File.ReadAllTextAsync(inputPath);

            // 反序列化 JSON 为字典
            var databaseData = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(json);

            if (databaseData == null)
            {
                Console.WriteLine("JSON 数据无效或为空。");
                return;
            }

            foreach (var tableData in databaseData)
            {
                var tableName = tableData.Key;
                var rows = tableData.Value;

                // 通过反射找到 DbSet 属性
                var dbSetProperty = this.GetType()
                    .GetProperties()
                    .FirstOrDefault(p => p.Name == tableName &&
                                         p.PropertyType.IsGenericType &&
                                         p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

                if (dbSetProperty == null)
                {
                    Logger.Warn($"未找到对应的 DbSet: {tableName}，跳过此表。");
                    continue;
                }

                // 获取 DbSet 泛型类型
                var entityType = dbSetProperty.PropertyType.GetGenericArguments()[0];

                // DbSet 实例
                var dbSet = dbSetProperty.GetValue(this);

                // 动态创建实体对象并设置属性
                foreach (var row in rows)
                {
                    var entity = Activator.CreateInstance(entityType);
                    foreach (var column in row)
                    {
                        var property = entityType.GetProperty(column.Key);
                        if (property != null && column.Value != null)
                        {
                            // 将 JSON 值转换为属性所需的类型
                            var value = Convert.ChangeType(column.Value, property.PropertyType);
                            property.SetValue(entity, value);
                        }
                    }

                    // 将实体添加到 DbSet
                    var addMethod = dbSet.GetType().GetMethod("Add");
                    addMethod.Invoke(dbSet, new[] { entity });
                }
            }

            // 保存更改到数据库
            await SaveChangesAsync();
            Logger.Info("数据库已成功导入。");
        }
        catch (Exception ex)
        {
            Logger.Error($"导入数据库失败：{ex}");
        }
    }
}
