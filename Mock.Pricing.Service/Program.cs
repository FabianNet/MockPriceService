using System;
using System.Diagnostics;
using System.Reflection;
using Autofac;
using Autofac.Integration.WebApi;
using Mock.Pricing.Service.Utils;
using Topshelf;
using Configuration = Mock.Pricing.Service.Utils.Configuration;

namespace Mock.Pricing.Service
{
    /// <summary>
    ///     Represents the application which can be executes as console application or as service.
    /// </summary>
    public sealed class Program
    {
        #region ----------------------- Private Methods -------------------------------------------

        private static void Main()
        {
            var conf = new Configuration(ExecutionEnvironment.Test);
            BootStrapDependencies(conf);
            HostFactory.Run(x =>
            {
                RunAsExtensions.RunAs(x, conf.ServiceUserName, conf.ServiceUserPasswordEncrypted);
                x.SetDescription("Calculates and provides calendar and product info data.");
                StartModeExtensions.StartManually(x);
                x.SetDisplayName(conf.ServiceDisplayName);
                x.SetServiceName(conf.ServiceName);
                ServiceExtensions.Service<PricingService>(x, s =>
                {
                    s.ConstructUsing(
                        name =>
                            new PricingService(ResolutionExtensions.Resolve<Configuration>(Container)));
                    ServiceConfiguratorExtensions.WhenStarted<PricingService>(s, tc => tc.OnStart());
                    ServiceConfiguratorExtensions.WhenStopped<PricingService>(s, tc => tc.OnStop());
                    ServiceConfiguratorExtensions.WhenShutdown<PricingService>(s, tc => tc.OnStop());
                });
                InstallHostConfiguratorExtensions.BeforeInstall(x, () => NetShellCmdLineTool.RegisterHttpNamespace(conf));
                UninstallHostConfiguratorExtensions.AfterUninstall(x, () => NetShellCmdLineTool.UnregisterHttpNamespace(conf));
            });
        }


        private static void UnRegisterHttpNamespace(Configuration conf, bool addAccess)
        {
            foreach (var portExtensionKvp in new[]
            {
                new Tuple<string, int>("http", conf.ServiceHttpPort),
                new Tuple<string, int>("https", conf.ServiceHttpsPort)
            })
            {
                using (var proc = new Process())
                {
                    proc.StartInfo.FileName = "netsh.exe";
                    // register '*' weak prefix wild card instead of strong '+' - see https://msdn.microsoft.com/en-us/library/aa364698(v=vs.85).aspx
                    proc.StartInfo.Arguments =
                        $"http {(addAccess ? "add" : "delete")} urlacl url={portExtensionKvp.Item1}://*:{portExtensionKvp.Item2}{conf.ServiceBaseUrl} user={conf.ServiceUserName}";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.Start();
                    proc.WaitForExit();
                }
            }
        }


        public static IContainer Container;

        private static void BootStrapDependencies(Configuration config)
        {
            var builder = new ContainerBuilder();
            // register singletons
            builder.RegisterInstance(config).As<Configuration>();
            // Register your Web API controllers.
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            // build container
            var container = builder.Build();
            Container = container;
        }

        #endregion
    }
}