using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Bookstore.Domain.Customers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace Bookstore.Web.Helpers
{
    /// <summary>
    /// ASP.NET Core middleware that provides local (non-Cognito) authentication.
    /// Replaces the OWIN-based OwinMiddleware that used HttpContext.Current.
    /// </summary>
    public class LocalAuthenticationMiddleware : IMiddleware
    {
        private const string UserId = "FB6135C7-1464-4A72-B74E-4B63D343DD09";

        private readonly ICustomerService _customerService;

        public LocalAuthenticationMiddleware(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path.StartsWithSegments("/Authentication/Login"))
            {
                var principal = BuildClaimsPrincipal();

                // Sign in with the local cookie scheme
                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                    });

                await SaveCustomerDetailsAsync(principal);

                context.Response.Redirect("/");
                return;
            }

            // If a local-auth cookie is already present, populate the principal
            if (context.Request.Cookies.ContainsKey("LocalAuthentication"))
            {
                var principal = BuildClaimsPrincipal();
                context.User = principal;
                await SaveCustomerDetailsAsync(principal);
            }

            await next(context);
        }

        private static ClaimsPrincipal BuildClaimsPrincipal()
        {
            var identity = new ClaimsIdentity("Application");
            identity.AddClaim(new Claim(ClaimTypes.Name, "bookstoreuser"));
            identity.AddClaim(new Claim("nameidentifier", UserId));
            identity.AddClaim(new Claim("given_name", "Bookstore"));
            identity.AddClaim(new Claim("family_name", "User"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Administrators"));
            return new ClaimsPrincipal(identity);
        }

        private async Task SaveCustomerDetailsAsync(ClaimsPrincipal principal)
        {
            var identity = (ClaimsIdentity)principal.Identity!;

            var dto = new CreateOrUpdateCustomerDto(
                identity.FindFirst("nameidentifier")!.Value,
                identity.Name!,
                identity.FindFirst("given_name")!.Value,
                identity.FindFirst("family_name")!.Value);

            await _customerService.CreateOrUpdateCustomerAsync(dto);
        }
    }
}
