using Microsoft.EntityFrameworkCore;
using RY.TransferImagePro.Domain.Entity;

namespace RY.TransferImagePro.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options)
            : base(options)
        {
            //Database.EnsureCreated();
        }

        public DbSet<ImageInformation> ImageInformations { get; set; }
    }
}