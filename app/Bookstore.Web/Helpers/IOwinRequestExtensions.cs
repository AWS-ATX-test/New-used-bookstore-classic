using Microsoft.AspNetCore.Owin;
using Microsoft.AspNetCore.Http;


namespace Bookstore.Web.Helpers
{
    public static class OwinRequestExtensions
    {
        public static string GetReturnUrl(this HttpContext context)
        {
            return $"{context.Request.Scheme}://{context.Request.Host}/signin-oidc";
        }
    }
}
