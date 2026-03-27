using Microsoft.AspNetCore.Http;

namespace Bookstore.Web.Helpers
{
    /// <summary>
    /// Extension methods for building redirect URIs from ASP.NET Core HttpRequest.
    /// Replaces the legacy IOwinRequest extension that referenced Microsoft.AspNetCore.Owin.
    /// </summary>
    public static class HttpRequestExtensions
    {
        public static string GetReturnUrl(this HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host}/signin-oidc";
        }
    }
}
