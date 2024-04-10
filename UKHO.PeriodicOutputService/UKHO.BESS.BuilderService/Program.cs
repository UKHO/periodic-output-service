using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
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
using UKHO.PeriodicOutputService.Common.Services;
using UKHO.PeriodicOutputService.Common.Utilities;

namespace UKHO.BESS.BuilderService
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static readonly string assemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
        private static IConfiguration configuration;

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
                     configuration.Bind("FSSApiConfiguration", fssApiConfiguration);
                 }

                 serviceCollection.AddDistributedMemoryCache();

                 serviceCollection.AddSingleton<IAuthFssTokenProvider, AuthTokenProvider>();
                 serviceCollection.AddSingleton<IAuthEssTokenProvider, AuthTokenProvider>();
                 serviceCollection.AddScoped<IBuilderService, Services.BuilderService>();
                 serviceCollection.AddScoped<IEssService, EssService>();
                 serviceCollection.AddScoped<IFssService, FssService>();
                 serviceCollection.AddHttpClient();
                 serviceCollection.AddTransient<IEssApiClient, EssApiClient>();
                 serviceCollection.AddTransient<IFssApiClient, FssApiClient>();

                 serviceCollection.AddScoped<IFileSystemHelper, FileSystemHelper>();
                 serviceCollection.AddScoped<IFileSystem, FileSystem>();
                 serviceCollection.AddScoped<IZipHelper, ZipHelper>();
                 serviceCollection.AddScoped<IFileUtility, FileUtility>();

                 serviceCollection.AddHttpClient("DownloadClient",
                         httpClient => httpClient.BaseAddress = new Uri(fssApiConfiguration.BaseUrl))
                     .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                     {
                         AllowAutoRedirect = false
                     }).SetHandlerLifetime(Timeout.InfiniteTimeSpan);
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
