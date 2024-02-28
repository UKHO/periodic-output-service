using Microsoft.Extensions.Configuration;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class TestConfiguration
    {
        protected IConfigurationRoot ConfigurationRoot;
        public BESSApiConfiguration bessConfig = new();

        public class BESSApiConfiguration
        {
            public string? BaseUrl { get; set; }
        }

        public TestConfiguration()
        {
            ConfigurationRoot = new ConfigurationBuilder()
                               .AddJsonFile("appsettings.json", false)
                               .Build();

            ConfigurationRoot.Bind("BESSApiConfiguration", bessConfig);
        }
    }
}
