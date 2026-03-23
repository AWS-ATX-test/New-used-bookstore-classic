
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

namespace Bookstore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add connection string and app settings from Web.config migration
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:BookstoreDatabaseConnection"] = "Server=(localdb)\\MSSQLLocalDB;Initial Catalog=BookStoreClassic;MultipleActiveResultSets=true;Integrated Security=SSPI;",
                ["Environment"] = "Development",
                ["Services:Authentication"] = "local",
                ["Services:Database"] = "local",
                ["Services:FileService"] = "local",
                ["Services:ImageValidationService"] = "local",
                ["Services:LoggingService"] = "local",
                ["Authentication:Cognito:LocalClientId"] = "[Retrieved from AWS Systems Manager Parameter Store when Services/Authentication == 'aws']",
                ["Authentication:Cognito:AppRunnerClientId"] = "[Retrieved from AWS Systems Manager Parameter Store when Services/Authentication == 'aws']",
                ["Authentication:Cognito:MetadataAddress"] = "[Retrieved from AWS Systems Manager Parameter Store when Services/Authentication == 'aws']",
                ["Authentication:Cognito:CognitoDomain"] = "[Retrieved from AWS Systems Manager Parameter Store when Services/Authentication == 'aws']",
                ["Files:BucketName"] = "[Retrieved from AWS Systems Manager Parameter Store when Services/FileService == 'aws']",
                ["Files:CloudFrontDomain"] = "[Retrieved from AWS Systems Manager Parameter Store when Services/FileService == 'aws']"
            });

            // Store configuration in static ConfigurationManager
            ConfigurationManager.Configuration = builder.Configuration;

            // Configure built-in logging (replaces NLog)
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            // Add services to the container (formerly ConfigureServices)
            builder.Services.AddControllersWithViews();
            //Added Services

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

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            //Added Middleware

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }

    public class ConfigurationManager
    {
        public static IConfiguration Configuration { get; set; }
    }
}