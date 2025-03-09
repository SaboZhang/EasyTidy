using EasyTidy.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Windows.UI;

namespace EasyTidy.Common.Database;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var DirPath = Constants.CnfPath;
            if (!Directory.Exists(DirPath))
            {
                Directory.CreateDirectory(DirPath);
            }
            var dbFile = @$"{DirPath}\EasyTidy.db";
            optionsBuilder.UseSqlite($"Data Source={dbFile}");
        }
    }


    public async Task InitializeDatabaseAsync()
    {
        var dirPath = Constants.CnfPath;
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        // 确保数据库已创建
        await Database.EnsureCreatedAsync();

        // 获取 SQL 脚本文件夹路径
        string scriptsFolderPath = Path.Combine(Constants.ExecutePath, "Assets", "Script");

        if (!Directory.Exists(scriptsFolderPath))
        {
            Logger.Error($"Scripts folder not found: {scriptsFolderPath}");
            return;
        }

        // 获取所有 .sql 文件并按文件名排序
        var scriptFiles = Directory.GetFiles(scriptsFolderPath, "*.sql").OrderBy(Path.GetFileName);

        foreach (var scriptFilePath in scriptFiles)
        {
            string scriptName = Path.GetFileNameWithoutExtension(scriptFilePath);

            // 检查脚本是否已经执行过
            if (!await ScriptExecutionStatus.AnyAsync(s => s.ScriptName == scriptName && s.Status == "executed"))
            {
                try
                {
                    // 检查是否需要执行此脚本
                    if (await NeedsToExecuteScriptAsync(scriptFilePath))
                    {
                        await ExecuteSqlScriptAsync(scriptFilePath);

                        // 记录脚本执行状态
                        ScriptExecutionStatus.Add(new ScriptExecutionStatus
                        {
                            ScriptName = scriptName,
                            Status = "executed",
                            ExecutionDate = DateTime.UtcNow
                        });

                        await SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to execute script {scriptName}: {ex.Message}");
                    throw;
                }
            }
        }

        Logger.Info("EasyTidy 默认数据初始化完成！");
    }

    /// <summary>
    /// 检查是否需要执行 SQL 脚本
    /// </summary>
    /// <param name="scriptFilePath">SQL 脚本路径</param>
    /// <returns>是否需要执行</returns>
    private async Task<bool> NeedsToExecuteScriptAsync(string scriptFilePath)
    {
        string scriptContent = await File.ReadAllTextAsync(scriptFilePath);
        var requiredFields = ExtractFieldsFromScript(scriptContent);

        foreach (var field in requiredFields)
        {
            if (await FieldExistsAsync(field.TableName, field.FieldName))
            {
                Logger.Info($"Field '{field.FieldName}' in table '{field.TableName}' already exists, skipping script.");
                return false; // 如果字段已存在，则跳过整个脚本
            }
        }

        return true; // 如果字段不存在，需要执行脚本
    }

    /// <summary>
    /// 检查表中的字段是否已存在
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="fieldName">字段名</param>
    /// <returns>字段是否存在</returns>
    private async Task<bool> FieldExistsAsync(string tableName, string fieldName)
    {
        var query = $"PRAGMA table_info({tableName});";

        await using var connection = Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = query;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader["name"].ToString(), fieldName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 从 SQL 脚本中提取表名和字段名
    /// </summary>
    /// <param name="scriptContent">SQL 脚本内容</param>
    /// <returns>提取的表名和字段名集合</returns>
    private IEnumerable<(string TableName, string FieldName)> ExtractFieldsFromScript(string scriptContent)
    {
        // 匹配 ALTER TABLE ... ADD COLUMN ... 的语法
        var matches = Regex.Matches(
            scriptContent,
            @"ALTER TABLE\s+(\w+)\s+ADD\s+COLUMN\s+(\w+)",
            RegexOptions.IgnoreCase);

        return matches.Select(match =>
            (TableName: match.Groups[1].Value, FieldName: match.Groups[2].Value));
    }

    private async Task ExecuteSqlScriptAsync(string scriptPath)
    {
        var scriptContent = await File.ReadAllTextAsync(scriptPath);
        await Database.ExecuteSqlRawAsync(scriptContent);
    }

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        ReferenceHandler = ReferenceHandler.Preserve,
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

    public DbSet<TaskOrchestrationTable> TaskOrchestration { get; set; }

    public DbSet<ScheduleTable> Schedule { get; set; }

    public DbSet<AutomaticTable> Automatic { get; set; }

    public DbSet<TaskGroupTable> TaskGroup { get; set; }

    public DbSet<FilterTable> Filters { get; set; }

    public DbSet<ScriptExecutionStatus> ScriptExecutionStatus { get; set; }

    public DbSet<AIServiceTable> AIService { get; set; }

}
