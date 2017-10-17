using System.Web.Http;
using System.Web.Http.Cors;
using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.Owin.Hosting;
using Owin;
using Swashbuckle.Application;

namespace Mock.Pricing.Service.Utils
{
    /// <summary>
    ///     Initializes the Web API host.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Startup
    {
        /// <summary>
        ///     Configurations the specified <see cref="IAppBuilder" /> (Web API service).
        ///     The <see cref="Startup" /> class is specified as parameter in the <see cref="WebApp.Start{TStartup}(string)" />
        ///     method.
        /// </summary>
        /// <param name="appBuilder">The application builder.</param>
        public void Configuration(IAppBuilder appBuilder)
        {
            var container = Program.Container;
            var conf = container.Resolve<Configuration>();
            var httpConfig = new HttpConfiguration();

            httpConfig.MapHttpAttributeRoutes();

            var cors = new EnableCorsAttribute("*", "*", "*");
            httpConfig.EnableCors(cors);

            var version = GetType().Assembly.GetName().Version;
            // the version string must not have any dots ('.') as some hosts will treat them as file extension separators and thus will bypass routing logic

           httpConfig.EnableSwagger(
               c => c.SingleApiVersion($"v{version.Major}_{version.Minor}_{version.Build}", "Pricing API"))
               .EnableSwaggerUi();
            httpConfig.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            // OWIN WEB API SETUP:
            // Register the Autofac middleware FIRST, then the Autofac Web API middleware,
            // and finally the standard Web API middleware.
            appBuilder.UseAutofacMiddleware(container);
            appBuilder.UseAutofacWebApi(httpConfig);

            appBuilder.UseWebApi(httpConfig);
        }
    }
}