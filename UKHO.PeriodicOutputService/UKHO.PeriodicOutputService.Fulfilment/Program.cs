using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using UKHO.Logging.EventHubLogProvider;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Services;

namespace UKHO.PeriodicOutputService.Fulfilment
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static readonly InMemoryChannel s_aIChannel = new();
        private static readonly string s_assemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;

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
            //Add logging
            serviceCollection.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));

                string instrumentationKey = configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                if (!string.IsNullOrEmpty(instrumentationKey))
                {
                    loggingBuilder.AddApplicationInsights(instrumentationKey);
                }

#if DEBUG
                loggingBuilder.AddSerilog(new LoggerConfiguration()
                                .WriteTo.File("Logs/UKHO.PeriodicOutputService.Fulfilment-Logs-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
                                .MinimumLevel.Information()
                                .MinimumLevel.Override("UKHO", LogEventLevel.Debug)
                                .CreateLogger(), dispose: true);
#endif

                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();

                EventHubLoggingConfiguration eventHubConfig = configuration.GetSection("EventHubLoggingConfiguration").Get<EventHubLoggingConfiguration>();

                if (!string.IsNullOrWhiteSpace(eventHubConfig.ConnectionString))
                {
                    loggingBuilder.AddEventHub(config =>
                    {
                        config.Environment = eventHubConfig.Environment;
                        config.DefaultMinimumLogLevel =
                            (LogLevel)Enum.Parse(typeof(LogLevel), eventHubConfig.MinimumLoggingLevel, true);
                        config.MinimumLogLevels["UKHO"] =
                            (LogLevel)Enum.Parse(typeof(LogLevel), eventHubConfig.UkhoMinimumLoggingLevel, true);
                        config.EventHubConnectionString = eventHubConfig.ConnectionString;
                        config.EventHubEntityPath = eventHubConfig.EntityPath;
                        config.System = eventHubConfig.System;
                        config.Service = eventHubConfig.Service;
                        config.NodeName = eventHubConfig.NodeName;
                        config.AdditionalValuesProvider = additionalValues =>
                        {
                            additionalValues["_AssemblyVersion"] = s_assemblyVersion;
                        };
                    });
                }
            });

            serviceCollection.Configure<TelemetryConfiguration>(
                (config) =>
                {
                    config.TelemetryChannel = s_aIChannel;
                }
            );

            if (configuration != null)
            {
                serviceCollection.Configure<FleetManagerB2BApiConfiguration>(configuration.GetSection("FleetManagerB2BApiConfiguration"));
                serviceCollection.Configure<EssManagedIdentityConfiguration>(configuration.GetSection("ESSManagedIdentityConfiguration"));
                serviceCollection.Configure<FssApiConfiguration>(configuration.GetSection("FSSApiConfiguration"));
                serviceCollection.Configure<ExchangeSetApiConfiguration>(configuration.GetSection("ESSApiConfiguration"));
            }

            var essAzureADConfiguration = new ExchangeSetApiConfiguration();
            configuration.Bind("ESSAzureADConfiguration", essAzureADConfiguration);

            serviceCollection.AddDistributedMemoryCache();

            serviceCollection.AddTransient<PosFulfilmentJob>();

            serviceCollection.AddSingleton<IAuthFssTokenProvider, AuthTokenProvider>();
            serviceCollection.AddSingleton<IAuthEssTokenProvider, AuthTokenProvider>();

            serviceCollection.AddScoped<IExchangeSetApiConfiguration, ExchangeSetApiConfiguration>();
            serviceCollection.AddScoped<IFleetManagerB2BApiConfiguration, FleetManagerB2BApiConfiguration>();

            serviceCollection.AddScoped<IFleetManagerService, FleetManagerService>();
            serviceCollection.AddScoped<IFulfilmentDataService, FulfilmentDataService>();
            serviceCollection.AddScoped<IExchangeSetApiService, ExchangeSetApiService>();
            serviceCollection.AddScoped<IFssBatchService, FssBatchService>();

            serviceCollection.AddHttpClient();
            serviceCollection.AddTransient<IExchangeSetApiClient, ExchangeSetApiClient>();
            serviceCollection.AddTransient<IFleetManagerApiClient, FleetManagerApiClient>();
            serviceCollection.AddTransient<IFssApiClient, FssApiClient>();

        }
    }
}
