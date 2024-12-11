using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Net.Http.Headers;
using System.Reflection;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using UKHO.BESS.BuilderService.Services;
using UKHO.Logging.EventHubLogProvider;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.PermitDecryption;
using UKHO.PeriodicOutputService.Common.Services;
using UKHO.PeriodicOutputService.Common.Utilities;
using UKHO.Torus.Core;
using UKHO.Torus.Enc.Core.EncCatalogue;

namespace UKHO.BESS.BuilderService
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static readonly string assemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
        private static IConfiguration configuration;
        private const string BESSBuilderService = "BESSBuilderService";

        public static async Task Main()
        {
            try
            {
                HostBuilder hostBuilder = BuildHostConfiguration();
                IHost host = hostBuilder.Build();

                using (host)
                {
                    await host.RunAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}{Environment.NewLine} Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static HostBuilder BuildHostConfiguration()
        {
            HostBuilder hostBuilder = new();
            hostBuilder.ConfigureAppConfiguration((hostContext, builder) =>
            {
                builder.AddJsonFile("appsettings.json", true, true);
                //Add environment specific configuration files.
                var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (!string.IsNullOrWhiteSpace(environmentName))
                {
                    builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
                }

#if DEBUG
                //Add development overrides configuration
                builder.AddJsonFile("appsettings.local.overrides.json", true, true);
#endif

                string kvServiceUri = builder.Build()["KeyVaultSettings:ServiceUri"];

                if (!string.IsNullOrWhiteSpace(kvServiceUri))
                {
                    var secretClient = new SecretClient(new Uri(kvServiceUri), new DefaultAzureCredential(
                                                            new DefaultAzureCredentialOptions()));
                    builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                }

                //Add environment variables
                builder.AddEnvironmentVariables();

                configuration = builder.Build();
            })
             .ConfigureLogging((hostContext, loggingBuilder) =>
             {
                 loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));

#if DEBUG
                 loggingBuilder.AddSerilog(new LoggerConfiguration()
                                 .WriteTo.File("Logs/UKHO.BESS.BuilderService-Logs-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
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
             })
             .ConfigureServices((hostContext, serviceCollection) =>
             {
                 serviceCollection.AddApplicationInsightsTelemetryWorkerService();
                 var fssApiConfiguration = new FssApiConfiguration();
                 if (configuration != null)
                 {
                     serviceCollection.AddSingleton(configuration);
                     serviceCollection.Configure<BessStorageConfiguration>(configuration.GetSection("BessStorageConfiguration"));
                     serviceCollection.Configure<EssManagedIdentityConfiguration>(configuration.GetSection("ESSManagedIdentityConfiguration"));
                     serviceCollection.Configure<FssApiConfiguration>(configuration.GetSection("FSSApiConfiguration"));
                     serviceCollection.Configure<EssApiConfiguration>(configuration.GetSection("ESSApiConfiguration"));
                     serviceCollection.Configure<AzureStorageConfiguration>(configuration.GetSection("BessStorageConfiguration"));
                     serviceCollection.Configure<PksApiConfiguration>(configuration.GetSection("PksApiConfiguration"));

                     configuration.Bind("FSSApiConfiguration", fssApiConfiguration);
                 }

                 serviceCollection.AddDistributedMemoryCache();

                 serviceCollection.AddSingleton<IAuthFssTokenProvider, AuthTokenProvider>();
                 serviceCollection.AddSingleton<IAuthEssTokenProvider, AuthTokenProvider>();
                 serviceCollection.AddScoped<IBuilderService, Services.BuilderService>();
                 serviceCollection.AddScoped<IEssService, EssService>();
                 serviceCollection.AddScoped<IFssService, FssService>();
                 serviceCollection.AddHttpClient();

                 serviceCollection.AddScoped<IFileSystemHelper, FileSystemHelper>();
                 serviceCollection.AddScoped<IFileSystem, FileSystem>();
                 serviceCollection.AddScoped<IZipHelper, ZipHelper>();
                 serviceCollection.AddScoped<IFileUtility, FileUtility>();
                 serviceCollection.AddScoped<IAzureTableStorageHelper, AzureTableStorageHelper>();
                 serviceCollection.AddScoped<IPermitDecryption, PermitDecryption>();
                 serviceCollection.AddScoped<IS63Crypt, S63Crypt>();
                 serviceCollection.AddScoped<ICatalog031Helper, Catalog031Helper>();
                 serviceCollection.AddScoped<IFactory<ICatalog031Builder>, Catalog031BuilderFactory>();
                 serviceCollection.AddScoped<PeriodicOutputService.Common.Helpers.ICatalog031ReaderFactory, Catalog031ReaderFactoryWrapper>();

                 serviceCollection.AddSingleton<IAuthPksTokenProvider, AuthTokenProvider>();
                 serviceCollection.AddScoped<IPksService, PksService>();

                 var retryCount = Convert.ToInt32(configuration["RetryConfiguration:RetryCount"]);
                 var sleepDuration = Convert.ToDouble(configuration["RetryConfiguration:SleepDuration"]);

                 serviceCollection.AddHttpClient<IFssApiClient, FssApiClient>("DownloadClient", httpClient =>
                 {
                     httpClient.BaseAddress = new Uri(configuration["FSSApiConfiguration:BaseUrl"]);
                     var productHeaderValue = new ProductInfoHeaderValue(BESSBuilderService, assemblyVersion);
                     httpClient.DefaultRequestHeaders.UserAgent.Add(productHeaderValue);
                 })
                 .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                 {
                     AllowAutoRedirect = false
                 }).SetHandlerLifetime(Timeout.InfiniteTimeSpan)
                  .AddPolicyHandler((services, request) =>
                     CommonHelper.GetRetryPolicy(services.GetService<ILogger<IFssApiClient>>(), "File Share", EventIds.RetryHttpClientFSSRequest, retryCount, sleepDuration));

                 serviceCollection.AddHttpClient<IEssApiClient, EssApiClient>("EssClient", client =>
                 {
                     client.BaseAddress = new Uri(configuration["ESSApiConfiguration:BaseUrl"]);
                     var productHeaderValue = new ProductInfoHeaderValue(BESSBuilderService, assemblyVersion);
                     client.DefaultRequestHeaders.UserAgent.Add(productHeaderValue);
                 }).AddPolicyHandler((services, request) =>
                     CommonHelper.GetRetryPolicy(services.GetService<ILogger<IEssApiClient>>(), "Exchange Set", EventIds.RetryHttpClientESSRequest, retryCount, sleepDuration));

                 serviceCollection.AddTransient<IEssApiClient, EssApiClient>();
                 serviceCollection.AddTransient<IFssApiClient, FssApiClient>();
                 serviceCollection.AddTransient<IPksApiClient, PksApiClient>();
             })
              .ConfigureWebJobs(b =>
              {
                  b.AddAzureStorageCoreServices()
                  .AddAzureStorageQueues()
                  .AddAzureStorageBlobs();
              });

            return hostBuilder;
        }
    }
}
