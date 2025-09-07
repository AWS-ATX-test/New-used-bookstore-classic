using Microsoft.AspNetCore.Owin;
using Microsoft.Owin;
using Owin;
using NLog;
using System;

[assembly: OwinStartup(typeof(Bookstore.Web.Startup))]

namespace Bookstore.Web
{
    public static class ConfigurationSetup
    {
        public static void ConfigureConfiguration()
        {
            // Add configuration setup code here
            // This is a placeholder implementation to fix the compilation error
        }
    }

    public static class DependencyInjectionSetup
    {
        public static void ConfigureDependencyInjection(IAppBuilder app)
        {
            // Add dependency injection setup code here
            // This is a placeholder implementation to fix the compilation error
        }
    }

    public static class AuthenticationConfig
    {
        public static void ConfigureAuthentication(IAppBuilder app)
        {
            // Add authentication setup code here
            // This is a placeholder implementation to fix the compilation error
        }
    }
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
// Configure logging directly instead of using LoggingSetup
            ConfigureLogging();

            ConfigurationSetup.ConfigureConfiguration();

            DependencyInjectionSetup.ConfigureDependencyInjection(app);

            AuthenticationConfig.ConfigureAuthentication(app);
        }

        private void ConfigureLogging()
        {
            try
            {
                // Basic NLog configuration
                var config = new NLog.Config.LoggingConfiguration();
                var logConsole = new NLog.Targets.ConsoleTarget("logconsole");

                config.AddRule(LogLevel.Info, LogLevel.Fatal, logConsole);
                LogManager.Configuration = config;

                // Note: Additional AWS Logger setup can be added here if needed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to configure logging: {ex.Message}");
            }
        }
    }
}
