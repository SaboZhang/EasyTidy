using EasyTidy.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EasyTidy.Common.Database;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
    {
        InitializeDatabaseAsync().Wait();
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
        await Database.EnsureCreatedAsync();
        await Database.MigrateAsync();

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
