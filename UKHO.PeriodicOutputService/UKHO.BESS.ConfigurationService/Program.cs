using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Reflection;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Elastic.Apm;
using Elastic.Apm.Azure.Storage;
using Elastic.Apm.DiagnosticSource;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using UKHO.BESS.ConfigurationService.Services;
using UKHO.BESS.ConfigurationService.Validation;
using UKHO.Logging.EventHubLogProvider;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Providers;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.ConfigurationService
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static readonly InMemoryChannel aiChannel = new();
        private static readonly string assemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
        private const string BESSConfigurationService = "BESSConfigurationService";

        public static async Task Main()
        {
            int delayTime = 5000;

            try
            {
                // Elastic APM
                Agent.Subscribe(new HttpDiagnosticsSubscriber());
                Agent.Subscribe(new AzureBlobStorageDiagnosticsSubscriber());

                //Build configuration
                IConfigurationRoot configuration = BuildConfiguration();

                var serviceCollection = new ServiceCollection();

                //Configure required services
                ConfigureServices(serviceCollection, configuration);

                ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
                try
                {
                    var bessConfigurationServiceJob = serviceProvider.GetService<BessConfigurationServiceJob>();
                    await bessConfigurationServiceJob.StartAsync();
                }
                finally
                {
                    //Ensure all buffered app insights logs are flushed into Azure
                    aiChannel.Flush();
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

#if DEBUG   //Add development overrides configuration
            configBuilder.AddJsonFile("appsettings.local.overrides.json", true, true);
#endif

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
            serviceCollection.AddApplicationInsightsTelemetryWorkerService();

            serviceCollection.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
#if DEBUG
                loggingBuilder.AddSerilog(new LoggerConfiguration()
                    .WriteTo.File("Logs/UKHO.BESS.ConfigurationService-Logs-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
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
                            additionalValues["_AssemblyVersion"] = assemblyVersion;
                        };
                    });
                }
            });

            serviceCollection.Configure<TelemetryConfiguration>(
                (config) =>
                {
                    config.TelemetryChannel = aiChannel;
                }
            );

            if (configuration != null)
            {
                serviceCollection.AddSingleton<IConfiguration>(configuration);
                serviceCollection.Configure<BessStorageConfiguration>(configuration.GetSection("BessStorageConfiguration"));
                serviceCollection.Configure<AzureStorageConfiguration>(configuration.GetSection("BessStorageConfiguration"));
                serviceCollection.Configure<SalesCatalogueConfiguration>(configuration.GetSection("SCSApiConfiguration"));
            }
            serviceCollection.AddDistributedMemoryCache();

            serviceCollection.AddSingleton<IAuthScsTokenProvider, AuthTokenProvider>();
            serviceCollection.AddSingleton<BessConfigurationServiceJob>();
            serviceCollection.AddScoped<IConfigurationService, Services.ConfigurationService>();
            serviceCollection.AddScoped<IAzureBlobStorageClient, AzureBlobStorageClient>();
            serviceCollection.AddScoped<IAzureTableStorageHelper, AzureTableStorageHelper>();
            serviceCollection.AddScoped<IConfigValidator, ConfigValidator>();
            serviceCollection.AddScoped<ISalesCatalogueService, SalesCatalogueService>();
            serviceCollection.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();
            serviceCollection.AddScoped<IAzureMessageQueueHelper, AzureMessageQueueHelper>();
            serviceCollection.AddScoped<IMacroTransformer, MacroTransformer>();
            serviceCollection.AddScoped<ICurrentDateTimeProvider, CurrentDateTimeProvider>();

            var retryCount = Convert.ToInt32(configuration["RetryConfiguration:RetryCount"]);
            var sleepDuration = Convert.ToDouble(configuration["RetryConfiguration:SleepDuration"]);

            serviceCollection.AddHttpClient<ISalesCatalogueClient, SalesCatalogueClient>("ScsClient", client =>
            {
                client.BaseAddress = new Uri(configuration["SCSApiConfiguration:BaseUrl"]);
                var productHeaderValue = new ProductInfoHeaderValue(BESSConfigurationService, assemblyVersion);
                client.DefaultRequestHeaders.UserAgent.Add(productHeaderValue);
            }).AddPolicyHandler((services, request) =>
                    CommonHelper.GetRetryPolicy(services.GetService<ILogger<ISalesCatalogueClient>>(), "Sales Catalogue", EventIds.RetryHttpClientScsRequest, retryCount, sleepDuration));

            serviceCollection.AddTransient<ISalesCatalogueClient, SalesCatalogueClient>();
        }
    }
}
