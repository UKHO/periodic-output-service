using Microsoft.Extensions.Configuration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class TestConfiguration
    {
        protected IConfigurationRoot ConfigurationRoot;
        public FleetManagerB2BApiConfiguration fleetManagerB2BConfig = new();
        public EssAuthorizationConfiguration EssAuthorizationConfig = new();
        public FunctionalTestFSSApiConfiguration FssConfig = new();

        public class FleetManagerB2BApiConfiguration
        {
            public string userName { get; set; }
            public string password { get; set; }
            public string baseUrl { get; set; }
            public string subscriptionKey { get; set; }
        }

        public class EssAuthorizationConfiguration
        {
            public string EssApiUrl { get; set; }
            public string MicrosoftOnlineLoginUrl { get; set; }
            public string TenantId { get; set; }
            public string AutoTestClientId { get; set; }
            public string AutoTestClientSecret { get; set; }
            public string EssClientId { get; set; }
            public bool IsRunningOnLocalMachine { get; set; }
        }

        public class FunctionalTestFSSApiConfiguration
        {
            public string BaseUrl { get; set; }
            public string FssClientId { get; set; }
            public bool IsRunningOnLocalMachine { get; set; }
            public string BatchStatusPollingCutoffTime { get; set; }
            public string BatchStatusPollingDelayTime { get; set; }
            public int BatchCommitWaitTime { get; set; }

        }

        public TestConfiguration()
        {
            ConfigurationRoot = new ConfigurationBuilder()
                               .AddJsonFile("appsettings.json", false)
                               .Build();

            ConfigurationRoot.Bind("FleetManagerB2BApiConfiguration", fleetManagerB2BConfig);
            ConfigurationRoot.Bind("EssAuthorizationConfiguration", EssAuthorizationConfig);
            ConfigurationRoot.Bind("FunctionalTestFSSApiConfiguration", FssConfig);
        }

    }
}
