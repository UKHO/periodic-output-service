using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace UKHO.AdmiraltyInformationOverlay.Fulfilment
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static async Task Main()
        {
            try
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddTransient<AioFulfilmentJob>();

                ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

                await serviceProvider.GetService<AioFulfilmentJob>().ProcessFulfilmentJob();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}{Environment.NewLine} Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
