
    using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Data.Entity.SqlServer;
using System.Data.Entity;

    namespace Bookstore
    {
        public class Program
        {
            public static void Main(string[] args)
            {
                var builder = WebApplication.CreateBuilder(args);

                // Add configuration sources
                builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                builder.Configuration.AddEnvironmentVariables();

                // Configure logging
                builder.Logging.ClearProviders();
                builder.Logging.AddConsole();
                builder.Logging.AddDebug();
                builder.Logging.AddEventSourceLogger();

                // Store configuration in static ConfigurationManager
                ConfigurationManager.Configuration = builder.Configuration;

                // Add connection strings from Web.config
                var connectionString = builder.Configuration.GetConnectionString("BookstoreDatabaseConnection")
                    ?? "Server=(localdb)\\MSSQLLocalDB;Initial Catalog=BookStoreClassic;MultipleActiveResultSets=true;Integrated Security=SSPI;";

                // Configure Entity Framework 6
                // Note: EF6 is configured via code rather than XML in .NET Core
                // The actual DbContext registration would be done in your service registration

                // Add services to the container (formerly ConfigureServices)
                builder.Services.AddControllersWithViews(options => {
                    // Register global filters (equivalent to FilterConfig.RegisterGlobalFilters)
                    // Add your filters here if needed
                })
                .AddNewtonsoftJson(options => {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });

                // Register areas
                builder.Services.AddMvc()
                    .AddControllersAsServices();

                // Add application settings from Web.config
                builder.Services.Configure<AppSettings>(options => {
                    options.Environment = builder.Configuration["Environment"] ?? "Development";
                    options.ServicesAuthentication = builder.Configuration["Services/Authentication"] ?? "local";
                    options.ServicesDatabase = builder.Configuration["Services/Database"] ?? "local";
                    options.ServicesFileService = builder.Configuration["Services/FileService"] ?? "local";
                    options.ServicesImageValidationService = builder.Configuration["Services/ImageValidationService"] ?? "local";
                    options.ServicesLoggingService = builder.Configuration["Services/LoggingService"] ?? "local";
                    options.AuthenticationCognitoLocalClientId = builder.Configuration["Authentication/Cognito/LocalClientId"];
                    options.AuthenticationCognitoAppRunnerClientId = builder.Configuration["Authentication/Cognito/AppRunnerClientId"];
                    options.AuthenticationCognitoMetadataAddress = builder.Configuration["Authentication/Cognito/MetadataAddress"];
                    options.AuthenticationCognitoCognitoDomain = builder.Configuration["Authentication/Cognito/CognitoDomain"];
                    options.FilesBucketName = builder.Configuration["Files/BucketName"];
                    options.FilesCloudFrontDomain = builder.Configuration["Files/CloudFrontDomain"];
                });

                // Add bundling services (equivalent to BundleConfig.RegisterBundles)
                // In ASP.NET Core, you would typically use WebOptimizer or other bundling solutions

                var app = builder.Build();

                // Configure the HTTP request pipeline (formerly Configure method)
                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseExceptionHandler("/Home/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
                }

                // Configure global error handling
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger("GlobalExceptionHandler");
                        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                        var exception = exceptionHandlerPathFeature?.Error;

                        logger.LogError(exception, "An unhandled exception occurred");

                        await context.Response.WriteAsync("An error occurred. Please try again later.");
                    });
                });

                app.UseHttpsRedirection();
                app.UseStaticFiles();

                app.UseRouting();

                app.UseAuthentication();
                app.UseAuthorization();

                app.UseEndpoints(endpoints =>
                {
                    // Register routes (equivalent to RouteConfig.RegisterRoutes)
                    endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");

                    // Area registration
                    endpoints.MapControllerRoute(
                        name: "areas",
                        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
                });

                app.Run();
            }
    }

    public class ConfigurationManager
    {
        public static IConfiguration Configuration { get; set; }
    }

    public class AppSettings
    {
        public string Environment { get; set; }
        public string ServicesAuthentication { get; set; }
        public string ServicesDatabase { get; set; }
        public string ServicesFileService { get; set; }
        public string ServicesImageValidationService { get; set; }
        public string ServicesLoggingService { get; set; }
        public string AuthenticationCognitoLocalClientId { get; set; }
        public string AuthenticationCognitoAppRunnerClientId { get; set; }
        public string AuthenticationCognitoMetadataAddress { get; set; }
        public string AuthenticationCognitoCognitoDomain { get; set; }
        public string FilesBucketName { get; set; }
        public string FilesCloudFrontDomain { get; set; }
    }
}
