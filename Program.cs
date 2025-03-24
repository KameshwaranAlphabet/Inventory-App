using Inventree_App.Context;
using Inventree_App.Service;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
            builder.Services.AddScoped<ICustomerService, CustomerService>();
            builder.Services.AddScoped<BackupService>(); // Background service
            builder.Services.AddScoped<DatabaseHelper>();
            // Configure JWT Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    };
                });
            // Add authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Home/Login"; // Set login page
                    options.AccessDeniedPath = "/Home/Index"; // Redirect unauthorized users
                });

            // Add authorization
            builder.Services.AddAuthorization();
            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddRazorPages();            // For Razor Pages

            var app = builder.Build();


            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Index");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            // Configure MySQL Database Context

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Apply migrations automatically
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                context.Database.Migrate(); // Apply migrations automatically
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
       
        }
    }
}
