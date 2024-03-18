using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using UKHO.Logging.EventHubLogProvider;
using UKHO.PeriodicOutputService.Common.Configuration;

namespace UKHO.BESS.BuilderService
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static readonly InMemoryChannel aiChannel = new();
        private static readonly string assemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
        private static IConfiguration ConfigurationBuilder;

        public static async Task Main()
        {
            int delayTime = 5000;

            try
            {
                try
                {
                    HostBuilder hostBuilder = BuildHostConfiguration();
                    IHost host = hostBuilder.Build();

                    using (host)
                    {
                        host.Run();
                    }
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

        private static HostBuilder BuildHostConfiguration()
        {
            HostBuilder hostBuilder = new();

            hostBuilder.ConfigureAppConfiguration((hostContext, builder) =>
            {
                //Add environment specific configuration files.
                string? environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (!string.IsNullOrWhiteSpace(environmentName))
                {
                    builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
                }

                string kvServiceUri = builder.Build()["KeyVaultSettings:ServiceUri"];
                if (!string.IsNullOrWhiteSpace(kvServiceUri))
                {
                    var secretClient = new SecretClient(new Uri(kvServiceUri), new DefaultAzureCredential(
                                                            new DefaultAzureCredentialOptions()));
                    builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                }

#if DEBUG   //Add development overrides configuration
                builder.AddJsonFile("appsettings.local.overrides.json", true, true);
#endif

                //Add environment variables
                builder.AddEnvironmentVariables();
                ConfigurationBuilder = builder.Build();
            })
                 .ConfigureLogging((hostContext, loggingBuilder) =>
                 {
                     loggingBuilder.AddConfiguration(ConfigurationBuilder.GetSection("Logging"));
#if DEBUG
                     loggingBuilder.AddSerilog(new LoggerConfiguration()
                         .WriteTo.File("Logs/UKHO.BESS.BuilderService-Logs-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
                         .MinimumLevel.Information()
                         .MinimumLevel.Override("UKHO", LogEventLevel.Debug)
                         .CreateLogger(), dispose: true);
#endif
                     loggingBuilder.AddConsole();
                     loggingBuilder.AddDebug();

                     EventHubLoggingConfiguration eventHubConfig = ConfigurationBuilder.GetSection("EventHubLoggingConfiguration").Get<EventHubLoggingConfiguration>();

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
                 .ConfigureServices((hostContext, services) =>
                 {
                     services.AddApplicationInsightsTelemetryWorkerService();

                     services.Configure<TelemetryConfiguration>(
                     (config) =>
                     {
                         config.TelemetryChannel = aiChannel;
                     });

                     if (ConfigurationBuilder != null)
                     {
                         services.AddSingleton<IConfiguration>(ConfigurationBuilder);
                     }
                 })
                 .ConfigureWebJobs(b =>
                {
                    b.AddAzureStorageCoreServices()
                    .AddAzureStorageQueues();
                });

            return hostBuilder;
        }
    }
}
