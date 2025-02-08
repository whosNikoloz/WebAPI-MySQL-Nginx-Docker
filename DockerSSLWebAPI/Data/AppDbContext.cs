using DockerSSLWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DockerSSLWebAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UsersModel> Users { get; set; }
    }
}
