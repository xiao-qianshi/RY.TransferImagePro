using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RY.TransferImagePro.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            //var configuration = new ConfigurationBuilder()
            //    .SetBasePath(basePath)
            //    .AddJsonFile("appsettings.json")
            //    .Build();
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            //var connectionString = configuration.GetConnectionString("Default");
            var connectionString = "Data Source=" + Path.Combine(basePath, "my-sqlite.db");
            //var connectionString = Path.Combine(basePath, configuration.GetConnectionString("Default"));
            //Path.Combine("Data Source=", basePath,
            //    configuration.GetConnectionString("Default").Replace("Data Source=", ""));
            /*.Replace("|DataDirectory|", Path.Combine(basePath, "wwwroot", "app_data"))*/
            
            builder.UseSqlite(connectionString);
            var db = new AppDbContext(builder.Options);
            //db.Database.EnsureCreated();
            return db;
        }
    }
}