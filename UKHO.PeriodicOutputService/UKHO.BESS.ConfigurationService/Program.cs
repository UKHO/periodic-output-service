using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Channel;

namespace UKHO.BESS.ConfigurationService
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static readonly InMemoryChannel s_aIChannel = new();
        static void Main()
        {
            try
            {
                Console.WriteLine("Started");
                //Build configuration
                IConfigurationRoot configuration = BuildConfiguration();

                var serviceCollection = new ServiceCollection();

                //Configure required services
                ConfigureServices(serviceCollection, configuration);
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

            //IConfigurationRoot tempConfig = configBuilder.Build();
            string kvServiceUri = configBuilder.Build()["KeyVaultSettings:ServiceUri"];
            if (!string.IsNullOrWhiteSpace(kvServiceUri))
            {
                var secretClient = new SecretClient(new Uri(kvServiceUri), new DefaultAzureCredential(
                                                        new DefaultAzureCredentialOptions()));
                configBuilder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
            }

            //Add environment variables
            configBuilder.AddEnvironmentVariables();

            return configBuilder.Build();
        }

        private static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddLogging(loggingBuilder =>
            {
                string instrumentationKey = configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                if (!string.IsNullOrEmpty(instrumentationKey))
                {
                    loggingBuilder.AddApplicationInsights(instrumentationKey);
                }
            });

            serviceCollection.Configure<TelemetryConfiguration>(
                (config) =>
                {
                    config.TelemetryChannel = s_aIChannel;
                }
            );
        }
    }
}
