using Movement.WebApp.Models.DataSources;
using Movement.WebApp.Models.SelfDeterminedCacheSystem;
using Movement.WebApp.Models;
using System.Data.Common;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Movement.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register EF DbContext using DefaultConnection from configuration
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<MovementEntities>(options => options.UseSqlServer(connectionString));

            builder.Services.AddScoped<IDbDataSource, SqlServerDbDataSource>();

            // Configure StackExchange.Redis ConnectionMultiplexer from configuration
            var redisConfig = builder.Configuration.GetSection("Redis:Configuration").Value ?? "localhost:6379";
            var multiplexer = ConnectionMultiplexer.Connect(redisConfig);
            builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);

            builder.Services.AddSingleton<IRedisDataSource, RedisDataSource>();
            builder.Services.AddSingleton<ISdcsDataSource, SelfDesignedCache>();
           
            builder.Services.AddScoped<IDataSource, DataServiceCoordinator>();

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, "Movement.WebApp.xml");
                c.IncludeXmlComments(xmlPath);
            });

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
