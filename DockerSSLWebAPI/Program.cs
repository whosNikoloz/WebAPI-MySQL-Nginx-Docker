using DockerSSLWebAPI.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System;
using System.Threading;

namespace DockerSSLWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            builder.Services.AddCors(builder =>
                builder.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

            var app = builder.Build();

            WaitForDatabase(connectionString, maxRetries: 30, delayMilliseconds: 5000);

            // Apply migrations only if needed
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (dbContext.Database.GetPendingMigrations().Any())
                {
                    dbContext.Database.Migrate();
                }
            }

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.UseCors();
            app.MapControllers();
            app.Run();
        }

        private static void WaitForDatabase(string connectionString, int maxRetries, int delayMilliseconds)
        {
            int retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    using var connection = new MySqlConnection(connectionString);
                    connection.Open();
                    Console.WriteLine("Successfully connected to MySQL.");
                    return;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Console.WriteLine($"Waiting for MySQL to be ready... Attempt {retryCount}/{maxRetries}: {ex.Message}");
                    Thread.Sleep(delayMilliseconds);
                }
            }
            throw new Exception("Unable to connect to MySQL after multiple attempts.");
        }
    }
}
