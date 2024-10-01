using EasyTidy.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
