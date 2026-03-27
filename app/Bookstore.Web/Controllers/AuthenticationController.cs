using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Bookstore.Web.Controllers
{
    [AllowAnonymous]
    public class AuthenticationController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthenticationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Login(string redirectUri = null)
        {
            if (string.IsNullOrWhiteSpace(redirectUri))
                return RedirectToAction("Index", "Home");

            return Redirect(redirectUri);
        }

        public IActionResult LogOut()
        {
            return _configuration["Services/Authentication"] == "aws"
                ? CognitoSignOut()
                : LocalSignOut();
        }

        private IActionResult LocalSignOut()
        {
            // Delete the local authentication cookie
            Response.Cookies.Delete("LocalAuthentication");

            return RedirectToAction("Index", "Home");
        }

        private IActionResult CognitoSignOut()
        {
            // Sign out of both the cookie and the OIDC session
            return SignOut(
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action("Index", "Home")
                },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}
