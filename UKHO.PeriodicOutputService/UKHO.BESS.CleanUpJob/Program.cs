using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using UKHO.BESS.CleanUpJob;

namespace UKHO.ExchangeSetService.CleanUpJob
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static string AssemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
        static InMemoryChannel aiChannel = new InMemoryChannel();

        static async Task Main()
        {
            try
            {
                var delayTime = 5000;

                //Build configuration
                var configuration = BuildConfiguration();

                var serviceCollection = new ServiceCollection();

                //Configure required services
                ConfigureServices(serviceCollection, configuration);

                //Create service provider. This will be used in logging.
                var serviceProvider = serviceCollection.BuildServiceProvider();

                try
                {
                    var cleanUpJob = serviceProvider.GetService<BespokeExchangeSetCleanUpJob>();
                    string transactionName = $"{System.Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")}-cleanup-transaction";

                    await Agent.Tracer.CaptureTransaction(transactionName, ApiConstants.TypeRequest, async () =>
                    {
                        //application code that is captured as a transaction
                        await cleanUpJob.ProcessCleanUp();
                    });
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
                Console.WriteLine($"Exception: {ex.Message}{Environment.NewLine}Stack trace: {ex.StackTrace}");
                Agent.Tracer.CurrentTransaction.CaptureException(ex);
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

            //Add environment variables
            configBuilder.AddEnvironmentVariables();

            return configBuilder.Build();
        }

        private static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddApplicationInsightsTelemetryWorkerService();

            //Add logging
            serviceCollection.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
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
            }

            serviceCollection.AddSingleton<BespokeExchangeSetCleanUpJob>();
        }
    }
}
