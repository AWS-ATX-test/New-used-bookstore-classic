using Microsoft.Extensions.Configuration;

namespace BobsBookstoreClassic.Data
{
    /// <summary>
    /// Thin adapter over ASP.NET Core IConfiguration that preserves the
    /// GetSetting / GetConnectionString API used across the application.
    /// Replaces the legacy version that relied on System.Configuration.ConfigurationManager.
    /// </summary>
    public sealed class BookstoreConfiguration
    {
        private static IConfiguration _configuration;

        /// <summary>
        /// Must be called once at application startup (e.g. in Program.cs) before
        /// any call to GetSetting / GetConnectionString.
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static string GetSetting(string key)
        {
            return _configuration?[key];
        }

        public static T GetSetting<T>(string key)
        {
            return _configuration!.GetValue<T>(key);
        }

        public static string GetConnectionString(string key)
        {
            return _configuration?.GetConnectionString(key);
        }
    }
}
