using EasyTidy.Model;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
        var DirPath = Constants.CnfPath;
        if (!Directory.Exists(DirPath))
        {
            Directory.CreateDirectory(DirPath);
        }
        await Database.EnsureCreatedAsync();

        if (!await ScriptExecutionStatus.AnyAsync(s => s.ScriptName == "quartz_sqlite" && s.Status == "executed"))
        {
            string scriptPath = Path.Combine(Constants.ExecutePath, "Assets", "Script", "quartz_sqlite.sql");
            ExecuteSqlScript(scriptPath);
            ScriptExecutionStatus.Add(new ScriptExecutionStatus
            {
                ScriptName = "quartz_sqlite",
                Status = "executed",
                ExecutionDate = DateTime.UtcNow
            });
            await SaveChangesAsync();
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

    private void ExecuteSqlScript(string scriptPath)
    {
        var script = File.ReadAllText(scriptPath);
        Database.ExecuteSqlRawAsync(script);
    }

    public DbSet<TaskOrchestrationTable> TaskOrchestration { get; set; }

    public DbSet<ScheduleTable> Schedule { get; set; }

    public DbSet<AutomaticTable> Automatic { get; set; }

    public DbSet<TaskGroupTable> TaskGroup { get; set; }

    public DbSet<FilterTable> Filters { get; set; }

    public DbSet<ScriptExecutionStatus> ScriptExecutionStatus { get; set; }

}
