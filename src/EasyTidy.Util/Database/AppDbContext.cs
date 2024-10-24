using EasyTidy.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;

namespace EasyTidy.Util;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
        Database.EnsureCreatedAsync();
        CreateScriptExecutionStatusTableIfNotExists();
        string scriptPath = $"{Constants.ExecutePath}\\Assets\\Script\\quartz_sqlite.sql";
        if (Database.GetPendingMigrations().Any())
        {
            Database.Migrate();
        }
        var scriptExecuted = CheckIfScriptExecuted();
        if (!scriptExecuted)
        {
            ExecuteSqlScript(this, scriptPath);
            MarkScriptAsExecuted();
        }
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

    private void CreateScriptExecutionStatusTableIfNotExists()
    {
        using var connection = new SqliteConnection($"Data Source={Constants.RootDirectoryPath}\\EasyTidy.db");
        connection.Open();
        var tableCheckCommand = connection.CreateCommand();
        tableCheckCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='script_execution_status';";
        var result = tableCheckCommand.ExecuteScalar();
        if (result == null)
        {
            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = "CREATE TABLE script_execution_status(" +
                "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                "script_name TEXT," +
                "status TEXT)";
            createTableCommand.ExecuteNonQuery();
        }
    }

    private bool CheckIfScriptExecuted()
    {
        using var connection = new SqliteConnection($"Data Source={Constants.RootDirectoryPath}\\EasyTidy.db");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM script_execution_status WHERE script_name = 'quartz_sqlite' AND status = 'executed'";
        var result = command.ExecuteReader();
        return result.HasRows;
    }

    private void MarkScriptAsExecuted()
    {
        using var connection = new SqliteConnection($"Data Source={Constants.RootDirectoryPath}\\EasyTidy.db");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO script_execution_status (script_name, status) VALUES ('quartz_sqlite', 'executed')";
        command.ExecuteNonQuery();
    }

    public DbSet<TaskOrchestrationTable> FileExplorer { get; set; }

    public DbSet<ScheduleTable> Schedule { get; set; }

    public DbSet<AutomaticTable> Automatic { get; set; }

    public DbSet<TaskGroupTable> TaskGroup { get; set; }

    public DbSet<FilterTable> Filters { get; set; }

}
