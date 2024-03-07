using Microsoft.Extensions.Configuration;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class TestConfiguration
    {
        protected IConfigurationRoot configurationRoot;
        public BessApiConfiguration bessConfig = new();
        public SharedKeyConfiguration sharedKeyConfig = new();

        public class BessApiConfiguration
        {
            public string? BaseUrl { get; set; }
            public string? ValidConfigPath { get; set; }
            public string? InvalidConfigPath { get; set; }
        }

        public class SharedKeyConfiguration
        {
            public string? Key { get; set; }
        }

        public TestConfiguration()
        {
            configurationRoot = new ConfigurationBuilder()
                               .AddJsonFile("appsettings.json", false)
                               .Build();

            configurationRoot.Bind("BESSApiConfiguration", bessConfig);
            configurationRoot.Bind("SharedKeyConfiguration", sharedKeyConfig);
        }
    }
}
