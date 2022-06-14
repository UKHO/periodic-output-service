using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#pragma warning disable S1128 // Unused "using" should be removed
#pragma warning disable S1128 // Unused "using" should be removed
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Services;
using UKHO.PeriodicOutputService.Common.Helpers;
using System.Net.Http.Headers;

namespace UKHO.PeriodicOutputService.Fulfilment
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static IConfiguration ConfigurationBuilder;
        private static string AssemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
        public const string PeriodicOutputServiceUserAgent = "PeriodicOutputService";

        public static void Main(string[] args)
        {
            HostBuilder hostBuilder = BuildHostConfiguration();

            IHost host = hostBuilder.Build();

            using (host)
            {
                host.Run();
            }
        }
        private static HostBuilder BuildHostConfiguration()
        {
            HostBuilder hostBuilder = new HostBuilder();
            hostBuilder.ConfigureAppConfiguration((hostContext, builder) =>
            {
                builder.AddJsonFile("appsettings.json");
                //Add environment specific configuration files.
                var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (!string.IsNullOrWhiteSpace(environmentName))
                {
                    builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
                }

                var tempConfig = builder.Build();
                string kvServiceUri = tempConfig["KeyVaultSettings:ServiceUri"];
                if (!string.IsNullOrWhiteSpace(kvServiceUri))
                {
                    var secretClient = new SecretClient(new Uri(kvServiceUri), new DefaultAzureCredential(
                                                        new DefaultAzureCredentialOptions { ManagedIdentityClientId = tempConfig["ESSManagedIdentity:ClientId"] }));
                    builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                }

#if DEBUG
                //Add development overrides configuration
                builder.AddJsonFile("appsettings.local.overrides.json", true, true);
#endif

                //Add environment variables
                builder.AddEnvironmentVariables();

                Program.ConfigurationBuilder = builder.Build();
            })
             .ConfigureServices((hostContext, services) =>
             {
                 var buildServiceProvider = services.BuildServiceProvider();

                 services.Configure<FleetManagerB2BApiConfiguration>(ConfigurationBuilder.GetSection("FleetManagerB2BApiConfiguration"));

                 services.AddScoped<IFleetManagerB2BApiConfiguration, FleetManagerB2BApiConfiguration>();
                 services.AddScoped<IFleetManagerService, FleetManagerService>();
                 services.AddScoped<IFulfilmentDataService, FulfilmentDataService>();

                 services.AddHttpClient<IFleetManagerClient, FleetManagerClient>(client =>
                 {
                     client.Timeout = TimeSpan.FromMinutes(Convert.ToDouble(10));
                 });
             })
              .ConfigureWebJobs(b =>
              {
                  b.AddAzureStorageCoreServices();
                  b.AddAzureStorage();
              });

            return hostBuilder;
        }
    }
}
