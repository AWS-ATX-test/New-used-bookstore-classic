
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
using System.Data.Entity.SqlServer;
using Newtonsoft.Json;

namespace Bookstore.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add configuration settings from Web.config
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            builder.Configuration.AddEnvironmentVariables();

            // Add connection string from Web.config
            var connectionString = builder.Configuration.GetConnectionString("BookstoreDatabaseConnection") ??
                "Server=(localdb)\\MSSQLLocalDB;Initial Catalog=BookStoreClassic;MultipleActiveResultSets=true;Integrated Security=SSPI;";

            // EntityFramework 6 is configured via DbConfiguration or connection string

            // Add services to the container
            builder.Services.AddControllersWithViews(options =>
            {
                // Register global filters directly
                // Add any required global filters here, for example:
                // options.Filters.Add(new AuthorizeFilter());
            })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            // Add Razor Pages support if needed
            builder.Services.AddRazorPages();

            // Add session support if needed
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add application settings from Web.config
            var environment = builder.Configuration["Environment"] ?? "Development";
            var authService = builder.Configuration["Services/Authentication"] ?? "local";
            var databaseService = builder.Configuration["Services/Database"] ?? "local";
            var fileService = builder.Configuration["Services/FileService"] ?? "local";
            var imageValidationService = builder.Configuration["Services/ImageValidationService"] ?? "local";
            var loggingService = builder.Configuration["Services/LoggingService"] ?? "local";

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Configure error handling (equivalent to Application_Error)
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                    var exception = exceptionHandlerPathFeature?.Error;

                    logger.LogError(exception, "Unhandled exception");

                    await Task.CompletedTask;
                });
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Register bundles (equivalent to BundleConfig.RegisterBundles)
            // Note: ASP.NET Core has different approaches for bundling and minification
// Consider using built-in tag helpers, WebOptimizer, or client-side bundling

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                // Register routes (equivalent to RouteConfig.RegisterRoutes)
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                // Register areas (equivalent to AreaRegistration.RegisterAllAreas)
                endpoints.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
                );

                endpoints.MapRazorPages();
            });

            app.Run();
        }
    }
}
