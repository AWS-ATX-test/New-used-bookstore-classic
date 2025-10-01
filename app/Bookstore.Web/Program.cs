
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Data.Entity;

namespace Bookstore.Web
{
    public class AppSettings
    {
        public string Environment { get; set; }
        public string ServicesAuthentication { get; set; }
        public string ServicesDatabase { get; set; }
        public string ServicesFileService { get; set; }
        public string ServicesImageValidationService { get; set; }
        public string ServicesLoggingService { get; set; }
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

            // Configure application settings from Web.config
            builder.Services.Configure<MvcOptions>(options =>
            {
                options.EnableEndpointRouting = true;
            });

            // Configure logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            // Add Entity Framework configuration
            var connectionString = builder.Configuration.GetConnectionString("BookstoreDatabaseConnection")
                ?? "Server=(localdb)\\MSSQLLocalDB;Initial Catalog=BookStoreClassic;MultipleActiveResultSets=true;Integrated Security=SSPI;";

            // Register application settings from Web.config
            builder.Services.Configure<AppSettings>(options => {
                options.Environment = builder.Configuration["Environment"] ?? "Development";
                options.ServicesAuthentication = builder.Configuration["Services/Authentication"] ?? "local";
                options.ServicesDatabase = builder.Configuration["Services/Database"] ?? "local";
                options.ServicesFileService = builder.Configuration["Services/FileService"] ?? "local";
                options.ServicesImageValidationService = builder.Configuration["Services/ImageValidationService"] ?? "local";
                options.ServicesLoggingService = builder.Configuration["Services/LoggingService"] ?? "local";
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment() ||
                app.Configuration["Environment"]?.ToLower() == "development")
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

            // Use session before routing
            app.UseSession();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Configure routes
            app.UseEndpoints(endpoints =>
            {
                // Register all areas
                endpoints.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                // Default route
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapRazorPages();
            });

            // Add global error handling
            app.Use(async (context, next) =>
            {
                try
                {
                    await next(context);
                }
                catch (Exception ex)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An unhandled exception occurred");
                    throw;
                }
            });

            app.Run();
        }
    }
}