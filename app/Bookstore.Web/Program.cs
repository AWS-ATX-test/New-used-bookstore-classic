
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data.Entity.SqlServer;
using Newtonsoft.Json;

namespace Bookstore.Web
{
    public class ClientValidationOptions
    {
        public bool Enabled { get; set; } = true;
    }

    public class ServiceOptions
    {
        public string Authentication { get; set; }
        public string Database { get; set; }
        public string FileService { get; set; }
        public string ImageValidationService { get; set; }
        public string LoggingService { get; set; }
    }
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add connection string from Web.config
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            builder.Configuration.AddEnvironmentVariables();

            // Add services to the container
            builder.Services.AddControllersWithViews()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });
            builder.Services.AddRazorPages();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add configuration settings from Web.config appSettings
            builder.Services.Configure<ClientValidationOptions>(options =>
            {
                options.Enabled = bool.Parse(builder.Configuration["ClientValidationEnabled"] ?? "true");
            });

            // Add environment settings
            builder.Environment.EnvironmentName = builder.Configuration["Environment"] ?? "Development";

            // Add service configuration
            builder.Services.Configure<ServiceOptions>(options =>
            {
                options.Authentication = builder.Configuration["Services/Authentication"] ?? "local";
                options.Database = builder.Configuration["Services/Database"] ?? "local";
                options.FileService = builder.Configuration["Services/FileService"] ?? "local";
                options.ImageValidationService = builder.Configuration["Services/ImageValidationService"] ?? "local";
                options.LoggingService = builder.Configuration["Services/LoggingService"] ?? "local";
            });

            // Add logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            // Entity Framework 6 configuration
            System.Data.Entity.Database.SetInitializer(new System.Data.Entity.CreateDatabaseIfNotExists<System.Data.Entity.DbContext>());

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapRazorPages();
            });

            app.Run();
        }
    }
}
