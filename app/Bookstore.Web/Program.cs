using System.Security.Claims;
using Amazon.Rekognition;
using Amazon.S3;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using BobsBookstoreClassic.Data;
using Bookstore.Data;
using Bookstore.Data.FileServices;
using Bookstore.Data.ImageResizeService;
using Bookstore.Data.ImageValidationServices;
using Bookstore.Data.Repositories;
using Bookstore.Domain;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using Bookstore.Web.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.AWS.Logger;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using Bookstore.Common;

namespace Bookstore.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // -----------------------------------------------------------------------
            // 1. CREATE BUILDER  (replaces Global.asax Application_Start +
            //    OWIN Startup.Configuration)
            // -----------------------------------------------------------------------
            var builder = WebApplication.CreateBuilder(args);

            // -----------------------------------------------------------------------
            // 2. CONFIGURATION  (replaces ConfigurationSetup.ConfigureConfiguration)
            //    appsettings.json is loaded automatically by WebApplication.CreateBuilder.
            //    Environment-variable overrides are also wired in automatically.
            //    AWS SSM parameters that were fetched at runtime in ConfigurationSetup
            //    are now expected to arrive via environment variables or a custom
            //    IConfigurationProvider - no synchronous SSM calls at startup.
            // -----------------------------------------------------------------------
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json",
                             optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configuration = builder.Configuration;

            // Initialize the legacy BookstoreConfiguration adapter so that any
            // remaining code using BookstoreConfiguration.GetSetting() resolves
            // values via the standard IConfiguration pipeline.
            BookstoreConfiguration.Initialize(configuration);

            // -----------------------------------------------------------------------
            // 3. LOGGING  (replaces LoggingSetup.ConfigureLogging)
            //    NLog with conditional AWS CloudWatch target mirrors the original
            //    LoggingSetup.ConfigureLogging() logic.
            // -----------------------------------------------------------------------
            builder.Logging.ClearProviders();

            var nlogConfig = new LoggingConfiguration();
            Target loggingTarget;

            if (configuration["Services/LoggingService"] == "aws")
            {
                loggingTarget = new AWSTarget { LogGroup = Constants.AppName };
            }
            else
            {
                loggingTarget = new DebuggerTarget("debugger");
            }

            nlogConfig.AddTarget("primary", loggingTarget);
            nlogConfig.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Info, loggingTarget));
            LogManager.Configuration = nlogConfig;

            builder.Logging.AddNLog(nlogConfig);

            // -----------------------------------------------------------------------
            // 4. AUTOFAC  (replaces DependencyInjectionSetup.ConfigureDependencyInjection)
            //    Use Autofac.Extensions.DependencyInjection as the service-provider
            //    factory so that the Autofac ContainerBuilder logic is preserved.
            // -----------------------------------------------------------------------
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
                // Services
                containerBuilder.RegisterType<BookService>().As<IBookService>();
                containerBuilder.RegisterType<OrderService>().As<IOrderService>();
                containerBuilder.RegisterType<ReferenceDataService>().As<IReferenceDataService>();
                containerBuilder.RegisterType<OfferService>().As<IOfferService>();
                containerBuilder.RegisterType<CustomerService>().As<ICustomerService>();
                containerBuilder.RegisterType<AddressService>().As<IAddressService>();
                containerBuilder.RegisterType<ShoppingCartService>().As<IShoppingCartService>();
                containerBuilder.RegisterType<ImageResizeService>().As<IImageResizeService>();

                // DbContext - InstancePerLifetimeScope replaces InstancePerRequest
                var connectionString = configuration.GetConnectionString("BookstoreDatabaseConnection")
                                       ?? configuration["ConnectionStrings:BookstoreDatabaseConnection"];
                containerBuilder.RegisterType<ApplicationDbContext>()
                    .WithParameter("connectionString", connectionString)
                    .InstancePerLifetimeScope();

                // Repositories
                containerBuilder.RegisterType<CustomerRepository>().As<ICustomerRepository>();
                containerBuilder.RegisterType<AddressRepository>().As<IAddressRepository>();
                containerBuilder.RegisterType<BookRepository>().As<IBookRepository>();
                containerBuilder.RegisterType<OfferRepository>().As<IOfferRepository>();
                containerBuilder.RegisterType<ShoppingCartRepository>().As<IShoppingCartRepository>();
                containerBuilder.RegisterType<OrderRepository>().As<IOrderRepository>();
                containerBuilder.RegisterType<ReferenceDataRepository>().As<IReferenceDataRepository>();

                containerBuilder.RegisterGeneric(typeof(PaginatedList<>))
                    .As(typeof(IPaginatedList<>))
                    .InstancePerLifetimeScope();

                // AWS / Local file service
                if (configuration["Services/FileService"] == "aws")
                {
                    containerBuilder.RegisterType<AmazonS3Client>().As<IAmazonS3>();
                    containerBuilder.RegisterType<S3FileService>().As<IFileService>();
                }
                else
                {
                    // wwwroot/Content is the static-files root in ASP.NET Core
                    containerBuilder.Register(ctx =>
                    {
                        var env = ctx.Resolve<IWebHostEnvironment>();
                        var webRootPath = System.IO.Path.Combine(env.WebRootPath, "Content");
                        return new LocalFileService(webRootPath);
                    }).As<IFileService>().SingleInstance();
                }

                // AWS / Local image-validation service
                if (configuration["Services/ImageValidationService"] == "aws")
                {
                    containerBuilder.RegisterType<AmazonRekognitionClient>().As<IAmazonRekognition>();
                    containerBuilder.RegisterType<RekognitionImageValidationService>().As<IImageValidationService>();
                }
                else
                {
                    containerBuilder.RegisterType<LocalImageValidationService>().As<IImageValidationService>();
                }
            });

            // -----------------------------------------------------------------------
            // 5. MVC / CONTROLLERS  (replaces FilterConfig.RegisterGlobalFilters)
            //    Global AuthorizeFilter moved to AddControllersWithViews options.
            //    Exception handling is handled by UseExceptionHandler in the pipeline.
            // -----------------------------------------------------------------------
            builder.Services.AddControllersWithViews(options =>
            {
                // Equivalent of filters.Add(new AuthorizeAttribute()) -
                // require authenticated users globally; controllers/actions that
                // allow anonymous access already carry [AllowAnonymous].
                options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
            });

            // IHttpContextAccessor is required by the ASP.NET Core port of
            // LocalAuthenticationMiddleware and HttpContextExtensions.
            builder.Services.AddHttpContextAccessor();

            // Register LocalAuthenticationMiddleware as a transient IMiddleware
            // so ASP.NET Core can activate it with DI (IMiddleware pattern).
            if (configuration["Services/Authentication"] != "aws")
            {
                builder.Services.AddTransient<LocalAuthenticationMiddleware>();
            }

            // -----------------------------------------------------------------------
            // 6. AUTHENTICATION  (replaces AuthenticationConfig.ConfigureAuthentication
            //    + OWIN UseCookieAuthentication / UseOpenIdConnectAuthentication)
            // -----------------------------------------------------------------------
            if (configuration["Services/Authentication"] == "aws")
            {
                builder.Services
                    .AddAuthentication(options =>
                    {
                        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    })
                    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                    {
                        options.ClientId = configuration["Authentication/Cognito/LocalClientId"];
                        options.MetadataAddress = configuration["Authentication/Cognito/MetadataAddress"];
                        options.ResponseType = "code";
                        options.UsePkce = true;
                        options.Scope.Clear();
                        options.Scope.Add("openid");
                        options.Scope.Add("profile");
                        options.SaveTokens = true;
                        options.UseTokenLifetime = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            NameClaimType = "cognito:username",
                            RoleClaimType = "cognito:groups"
                        };
                        // Redirect-URI built at runtime (mirrors original GetReturnUrl logic)
                        options.Events = new OpenIdConnectEvents
                        {
                            OnRedirectToIdentityProvider = ctx =>
                            {
                                var req = ctx.HttpContext.Request;
                                ctx.ProtocolMessage.RedirectUri =
                                    $"{req.Scheme}://{req.Host}/signin-oidc";
                                return System.Threading.Tasks.Task.CompletedTask;
                            },
                            OnAuthorizationCodeReceived = ctx =>
                            {
                                var req = ctx.HttpContext.Request;
                                ctx.TokenEndpointRequest!.RedirectUri =
                                    $"{req.Scheme}://{req.Host}/signin-oidc";
                                return System.Threading.Tasks.Task.CompletedTask;
                            },
                            OnTokenValidated = async ctx =>
                            {
                                // Resolve ICustomerService from the DI container and
                                // persist / update the authenticated customer record.
                                var service = ctx.HttpContext.RequestServices
                                    .GetRequiredService<ICustomerService>();

                                var identity = (ClaimsIdentity)ctx.Principal!.Identity!;

                                var dto = new CreateOrUpdateCustomerDto(
                                    identity.GetSub(),
                                    identity.Name,
                                    identity.FindFirst(c => c.Type.Contains("givenname"))!.Value,
                                    identity.FindFirst(c => c.Type.Contains("surname"))!.Value);

                                await service.CreateOrUpdateCustomerAsync(dto);
                            }
                        };
                    });
            }
            else
            {
                // Local auth: cookie only; actual principal is set by
                // LocalAuthenticationMiddleware (see middleware pipeline below).
                builder.Services
                    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                    {
                        options.Cookie.Name = "LocalAuthentication";
                        options.ExpireTimeSpan = System.TimeSpan.FromDays(1);
                    });
            }

            builder.Services.AddAuthorization();

            // -----------------------------------------------------------------------
            // 7. BUILD THE APPLICATION
            // -----------------------------------------------------------------------
            var app = builder.Build();

            // -----------------------------------------------------------------------
            // 8. MIDDLEWARE PIPELINE  (replaces OWIN pipeline + Global.asax)
            //    Order mirrors the original OWIN pipeline and ASP.NET Core best
            //    practices: exception handling -> HSTS -> HTTPS -> static files ->
            //    routing -> authentication -> authorisation -> endpoints.
            // -----------------------------------------------------------------------
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Equivalent of FilterConfig HandleErrorAttribute for production
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Static files served directly from wwwroot -
            // replaces BundleConfig (bundling/minification not available in
            // ASP.NET Core; scripts/styles referenced directly from wwwroot).
            app.UseStaticFiles();

            app.UseRouting();

            // Local auth middleware (ASP.NET Core IMiddleware port) runs before the
            // standard authentication middleware so it can populate the principal
            // from the LocalAuthentication cookie before UseAuthentication reads it.
            if (configuration["Services/Authentication"] != "aws")
            {
                app.UseMiddleware<LocalAuthenticationMiddleware>();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            // -----------------------------------------------------------------------
            // 9. ROUTING  (replaces RouteConfig.RegisterRoutes +
            //    AdminAreaRegistration.RegisterArea)
            //    Area controllers already carry [Area("Admin")] via [RouteArea] on
            //    AdminAreaControllerBase, so MapAreaControllerRoute wires them up.
            // -----------------------------------------------------------------------
            app.MapControllerRoute(
                name: "admin_default",
                pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}",
                defaults: new { area = "Admin" },
                constraints: null,
                dataTokens: new { area = "Admin" });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
