using Microsoft.Extensions.Configuration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class TestConfiguration
    {
        protected IConfigurationRoot ConfigurationRoot;
        private readonly IConfiguration configuration;
        public FleetManagerB2BApiConfiguration fleetManagerB2BConfig = new();
        public ESSApiConfiguration EssAuthorizationConfig = new();
        public FSSApiConfiguration FssConfig = new();
        public POSWebjobApiConfiguration POSWebJobConfig = new();
        public string HomeDirectory;

        public class FleetManagerB2BApiConfiguration
        {
            public string userName { get; set; }
            public string password { get; set; }
            public string baseUrl { get; set; }
            public string subscriptionKey { get; set; }
        }

        public class ESSApiConfiguration
        {
            public string BaseUrl { get; set; }
            public string MicrosoftOnlineLoginUrl { get; set; }
            public string TenantId { get; set; }
            public string AutoTestClientId { get; set; }
            public string AutoTestClientSecret { get; set; }
            public string EssClientId { get; set; }
            public bool IsRunningOnLocalMachine { get; set; }
        }

        public class FSSApiConfiguration
        {
            public string BaseUrl { get; set; }
            public string FssClientId { get; set; }
            public bool IsRunningOnLocalMachine { get; set; }
        }

        public class POSWebjobApiConfiguration
        {
            public string userName { get; set; }
            public string password { get; set; }
            public string baseUrl { get; set; }
            public string invalidPOSWebJobuserCredentialsBytes { get; set; }
        }

        public TestConfiguration()
        {
            ConfigurationRoot = new ConfigurationBuilder()
                               .AddJsonFile("appsettings.json", false)
                               .Build();


            configuration = new ConfigurationBuilder()
                               .AddJsonFile("appsettings.json", false)
                               .Build();

            ConfigurationRoot.Bind("FleetManagerB2BApiConfiguration", fleetManagerB2BConfig);
            ConfigurationRoot.Bind("ESSApiConfiguration", EssAuthorizationConfig);
            ConfigurationRoot.Bind("FSSApiConfiguration", FssConfig);
            ConfigurationRoot.Bind("POSWebjobApiConfiguration", POSWebJobConfig);
            HomeDirectory = configuration["HOME"];
        }

    }
}
