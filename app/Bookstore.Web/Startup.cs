// Startup.cs is retained as an Autofac module configurator.
// The [assembly: OwinStartup] attribute and the OWIN IAppBuilder pipeline
// have been removed and fully migrated to Program.cs.
//
// To add Autofac modules, register them inside the
// builder.Host.ConfigureContainer<ContainerBuilder>(...) call in Program.cs,
// or create Autofac Module classes and load them there.

namespace Bookstore.Web
{
    /// <summary>
    /// Placeholder retained for Autofac module configuration.
    /// All application startup logic has been consolidated into Program.cs.
    /// </summary>
    public sealed class BookstoreAutofacModules
    {
        // Add Autofac Module registrations here if the project is split into
        // feature-based modules in the future.
        // Example:
        //   public static void RegisterModules(ContainerBuilder builder)
        //   {
        //       builder.RegisterModule<DataModule>();
        //       builder.RegisterModule<DomainModule>();
        //   }
    }
}
