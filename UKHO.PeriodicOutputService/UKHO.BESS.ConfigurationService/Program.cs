using System.Diagnostics.CodeAnalysis;

namespace UKHO.BESS.ConfigurationService
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        static void Main()
        {
            try
            {
                Console.WriteLine("Started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}{Environment.NewLine} Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
