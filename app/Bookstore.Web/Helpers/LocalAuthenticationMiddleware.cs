using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Bookstore.Domain.Customers;
using Microsoft.AspNetCore.Owin;

using Microsoft.AspNetCore.Http;


namespace Bookstore.Web.Helpers
{
public class LocalAuthenticationMiddleware
{
    private const string UserId = "FB6135C7-1464-4A72-B74E-4B63D343DD09";

    private readonly ICustomerService _customerService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly RequestDelegate _next;

    public LocalAuthenticationMiddleware(ICustomerService customerService, IHttpContextAccessor httpContextAccessor, RequestDelegate next)
    {
        _customerService = customerService;
        _httpContextAccessor = httpContextAccessor;
        _next = next;
    }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Value.StartsWith("/Authentication/Login"))
            {
                CreateClaimsPrincipal(context);

                await SaveCustomerDetailsAsync(context);

                var cookieOptions = new CookieOptions { Expires = DateTime.Now.AddDays(1) };
                context.Response.Cookies.Append("LocalAuthentication", "bookstoreuser", cookieOptions);

                context.Response.Redirect("/");
            }
            else if (_httpContextAccessor.HttpContext.Request.Cookies["LocalAuthentication"] != null)
            {
                CreateClaimsPrincipal(context);

                await SaveCustomerDetailsAsync(_httpContextAccessor.HttpContext);

                await _next(context);
            }
            else
            {
                await _next(context);
            }
        }

        private void CreateClaimsPrincipal(HttpContext context)
        {
            var identity = new ClaimsIdentity("Application");

            identity.AddClaim(new Claim(ClaimTypes.Name, "bookstoreuser"));
            identity.AddClaim(new Claim("nameidentifier", UserId));
            identity.AddClaim(new Claim("given_name", "Bookstore"));
            identity.AddClaim(new Claim("family_name", "User"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Administrators"));

            context.User = new ClaimsPrincipal(identity);
        }

        private async Task SaveCustomerDetailsAsync(HttpContext context)
        {
            var identity = (ClaimsIdentity)context.User.Identity;

            var dto = new CreateOrUpdateCustomerDto(
                identity.FindFirst("nameidentifier").Value,
                identity.Name,
                identity.FindFirst("given_name").Value,
                identity.FindFirst("family_name").Value);

            await _customerService.CreateOrUpdateCustomerAsync(dto);
        }
    }
}
