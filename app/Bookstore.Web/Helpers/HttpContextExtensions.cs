using System;
using Microsoft.AspNetCore.Http;

namespace Bookstore.Web.Helpers
{
    /// <summary>
    /// ASP.NET Core extension methods for HttpContext.
    /// Replaces the legacy version that used System.Web.HttpCookie.
    /// </summary>
    public static class HttpContextExtensions
    {
        private const string CookieKey = "ShoppingCartId";

        public static string GetShoppingCartCorrelationId(this HttpContext context)
        {
            var shoppingCartClientId = context.Request.Cookies[CookieKey];

            if (string.IsNullOrWhiteSpace(shoppingCartClientId))
            {
                shoppingCartClientId = context.User.Identity?.IsAuthenticated == true
                    ? context.User.GetSub()
                    : Guid.NewGuid().ToString();
            }

            context.Response.Cookies.Append(
                CookieKey,
                shoppingCartClientId!,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    Path = "/"
                });

            return shoppingCartClientId!;
        }
    }
}
