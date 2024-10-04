using EasyTidy.Model;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace EasyTidy.Util;

public class AppDbContext : DbContext
{
    public AppDbContext()
    {
        Database.EnsureCreatedAsync();
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

    public DbSet<FileExplorerTable> FileExplorer { get; set; }

    public DbSet<ScheduleTable> Schedule { get; set; }

    public DbSet<AutomaticTable> Automatic { get; set; }
}
