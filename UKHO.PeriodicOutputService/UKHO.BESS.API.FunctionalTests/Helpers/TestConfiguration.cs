using Microsoft.Extensions.Configuration;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class TestConfiguration
    {
        protected IConfigurationRoot configurationRoot;
        public BessApiConfiguration bessConfig = new();

        public class BessApiConfiguration
        {
            public string? BaseUrl { get; set; }
        }

        public TestConfiguration()
        {
            configurationRoot = new ConfigurationBuilder()
                               .AddJsonFile("appsettings.json", false)
                               .Build();

            configurationRoot.Bind("BESSApiConfiguration", bessConfig);
        }
    }
}
