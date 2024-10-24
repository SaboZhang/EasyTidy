using EasyTidy.Model;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace EasyTidy.Util;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
        Database.EnsureCreatedAsync();
        string scriptPath = $"{Constants.ExecutePath}\\Assets\\Script\\quartz_sqlite.sql";
        ExecuteSqlScript(this, scriptPath);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var DirPath = Constants.RootDirectoryPath;
        if (!Directory.Exists(DirPath))
        {
            Directory.CreateDirectory(DirPath);
        }
        var dbFile = @$"{DirPath}\EasyTidy.db";
        optionsBuilder.UseSqlite($"Data Source={dbFile}");
    }

    private static void ExecuteSqlScript(AppDbContext context, string scriptPath)
    {
        string script = File.ReadAllText(scriptPath);
        context.Database.ExecuteSqlRaw(script);
    }

    public DbSet<TaskOrchestrationTable> FileExplorer { get; set; }

    public DbSet<ScheduleTable> Schedule { get; set; }

    public DbSet<AutomaticTable> Automatic { get; set; }

    public DbSet<TaskGroupTable> TaskGroup { get; set; }

    public DbSet<FilterTable> Filters { get; set; }

}
