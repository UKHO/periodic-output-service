using System.Reflection;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using UKHO.Logging.EventHubLogProvider;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Models.Configuration;

namespace UKHO.PeriodicOutputService.Fulfilment
{
    public static class Program
    {
        private static IConfiguration? s_configurationBuilder;
        private static readonly string s_assemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
        static readonly InMemoryChannel s_aiChannel = new();

        static async Task Main()
        {
            try
            {
                int delayTime = 5000;

                HostBuilder builder = BuildHostConfiguration();
                IHost host = builder.Build();
                using (host)
                {
                    if (host.Services.GetService(typeof(IJobHost)) is JobHost jobHost)
                    {
                        try
                        {
                            await host.StartAsync();
                            await jobHost.CallAsync("ProcessWebJob");
                            await host.StopAsync();
                        }
                        finally
                        {
                            //Ensure all buffered app insights logs are flushed into Azure
                            s_aiChannel.Flush();
                            await Task.Delay(delayTime);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Exception: jobHost is null");
                    }
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
            HostBuilder hostBuilder = new HostBuilder();
            hostBuilder.ConfigureAppConfiguration((hostContext, builder) =>
            {
                builder.AddJsonFile("appsettings.json");

                //Add environment specific configuration files.
                string? environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (!string.IsNullOrWhiteSpace(environmentName))
                {
                    builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
                }

                IConfigurationRoot tempConfig = builder.Build();
                string kvServiceUri = tempConfig["KeyVaultSettings:ServiceUri"];
                if (!string.IsNullOrWhiteSpace(kvServiceUri))
                {
                    var secretClient = new SecretClient(new Uri(kvServiceUri), new DefaultAzureCredential(
                                                        new DefaultAzureCredentialOptions { ManagedIdentityClientId = tempConfig["POSManagedIdentity:ClientId"] }));
                    builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                }

#if DEBUG
                //Add development overrides configuration
                builder.AddJsonFile("appsettings.local.overrides.json", true, true);
#endif

                //Add environment variables
                builder.AddEnvironmentVariables();

                Program.s_configurationBuilder = builder.Build();
            })
            .ConfigureLogging((hostContext, builder) =>
            {
                builder.AddConfiguration(s_configurationBuilder?.GetSection("Logging"));

#if DEBUG
                builder.AddSerilog(new LoggerConfiguration()
                                .WriteTo.File("Logs/UKHO.PeriodicOutputService.Fulfilment.Logs-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
                                .MinimumLevel.Information()
                                .MinimumLevel.Override("UKHO", LogEventLevel.Information)
                                .CreateLogger(), dispose: true);
#endif

                builder.AddConsole();

                //Add Application Insights if needed(if key exists in settings)
                string? instrumentationKey = s_configurationBuilder?["APPINSIGHTS_INSTRUMENTATIONKEY"];
                if (!string.IsNullOrEmpty(instrumentationKey))
                {
                    builder.AddApplicationInsightsWebJobs(o => o.InstrumentationKey = instrumentationKey);
                }

                EventHubLoggingConfiguration? eventhubConfig = s_configurationBuilder?.GetSection("EventHubLoggingConfiguration").Get<EventHubLoggingConfiguration>();

                if (!string.IsNullOrWhiteSpace(eventhubConfig?.ConnectionString))
                {
                    builder.AddEventHub(config =>
                    {
                        config.Environment = eventhubConfig.Environment;
                        config.DefaultMinimumLogLevel =
                            (LogLevel)Enum.Parse(typeof(LogLevel), eventhubConfig.MinimumLoggingLevel, true);
                        config.MinimumLogLevels["UKHO"] =
                            (LogLevel)Enum.Parse(typeof(LogLevel), eventhubConfig.UkhoMinimumLoggingLevel, true);
                        config.EventHubConnectionString = eventhubConfig.ConnectionString;
                        config.EventHubEntityPath = eventhubConfig.EntityPath;
                        config.System = eventhubConfig.System;
                        config.Service = eventhubConfig.Service;
                        config.NodeName = eventhubConfig.NodeName;
                        config.AdditionalValuesProvider = additionalValues =>
                        {
                            additionalValues["_AssemblyVersion"] = s_assemblyVersion;
                        };
                    });
                }
            })
            .ConfigureServices((hostContext, services) =>
            {
                //services.BuildServiceProvider();

                services.Configure<TelemetryConfiguration>(
                (config) =>
                {
                    config.TelemetryChannel = s_aiChannel;
                }
            );

                services.Configure<QueuesOptions>(s_configurationBuilder?.GetSection("QueuesOptions"));
                services.AddScoped<IAzureStorageConfiguration, AzureStorageConfiguration>();
                services.Configure<AzureStorageConfiguration>(s_configurationBuilder?.GetSection("POSAzureStorageConfiguration"));
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
