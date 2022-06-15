using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#pragma warning disable S1128 // Unused "using" should be removed
#pragma warning disable S1128 // Unused "using" should be removed
using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Services;
using UKHO.PeriodicOutputService.Common.Helpers;
using Microsoft.ApplicationInsights.Channel;

namespace UKHO.PeriodicOutputService.Fulfilment
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static readonly InMemoryChannel s_aIChannel = new();

        public static async Task Main()
        {
            try
            {
                int delayTime = 5000;

                //Build configuration
                IConfigurationRoot? configuration = BuildConfiguration();

                var serviceCollection = new ServiceCollection();

                //Configure required services
                ConfigureServices(serviceCollection, configuration);

                //Create service provider. This will be used in logging.
                ServiceProvider? serviceProvider = serviceCollection.BuildServiceProvider();

                try
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    await serviceProvider.GetService<PosFulfilmentJob>().ProcessFulfilmentJob();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }
                finally
                {
                    //Ensure all buffered app insights logs are flushed into Azure
                    s_aIChannel.Flush();
                    await Task.Delay(delayTime);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}{Environment.NewLine} Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true);

            //Add environment specific configuration files.
            string? environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!string.IsNullOrWhiteSpace(environmentName))
            {
                configBuilder.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
            }

            IConfigurationRoot tempConfig = configBuilder.Build();
            string kvServiceUri = tempConfig["KeyVaultSettings:ServiceUri"];
            if (!string.IsNullOrWhiteSpace(kvServiceUri))
            {
                var secretClient = new SecretClient(new Uri(kvServiceUri), new DefaultAzureCredential(
                                                        new DefaultAzureCredentialOptions()));
                configBuilder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
            }
#if DEBUG
            //Add development overrides configuration
            configBuilder.AddJsonFile("appsettings.local.overrides.json", true, true);
#endif

            //Add environment variables
            configBuilder.AddEnvironmentVariables();

            return configBuilder.Build();
        }

        private static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            if (configuration != null)
                serviceCollection.Configure<FleetManagerB2BApiConfiguration>(configuration.GetSection("FleetManagerB2BApiConfiguration"));

            serviceCollection.AddTransient<PosFulfilmentJob>();

            serviceCollection.AddScoped<IFleetManagerB2BApiConfiguration, FleetManagerB2BApiConfiguration>();
            serviceCollection.AddScoped<IFleetManagerService, FleetManagerService>();
            serviceCollection.AddScoped<IFulfilmentDataService, FulfilmentDataService>();

            serviceCollection.AddHttpClient<IFleetManagerClient, FleetManagerClient>(client =>
            {
                client.MaxResponseContentBufferSize = 2147483647;
                client.Timeout = TimeSpan.FromMinutes(Convert.ToDouble(5));
            });
        }
    }
}
