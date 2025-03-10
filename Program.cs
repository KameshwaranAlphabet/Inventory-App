using Inventree_App.Context;
using Inventree_App.Service;
using Microsoft.EntityFrameworkCore;
using System;

namespace Inventree_App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Get connection string from appsettings.json
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Register ApplicationContext BEFORE builder.Build()
            builder.Services.AddDbContext<ApplicationContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
            ;
            builder.Services.AddScoped<DatabaseHelper>();

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddRazorPages();            // For Razor Pages

            var app = builder.Build();
        

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            // Configure MySQL Database Context

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
