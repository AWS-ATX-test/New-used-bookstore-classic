using Microsoft.AspNetCore.Mvc;


namespace Bookstore.Web.Areas
{
    public class AdminAreaRegistration
    {
        public const string AreaName = "Admin";

        // Area registration is now handled in Program.cs via endpoint routing        // Example configuration:
        // endpoints.MapAreaControllerRoute("Admin_default", "Admin_default", "{controller=Home}/{action=Index}/{id?}");
    }
}
