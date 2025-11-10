
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bookstore.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add connection strings
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

            // Add services to the container
            builder.Services.AddControllersWithViews();

            // Configure logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.AddEventSourceLogger();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            // Global error logging middleware
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An unhandled exception occurred");
                    throw;
                }
            });

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