using Microsoft.Extensions.Configuration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class TestConfiguration
    {
        protected IConfigurationRoot ConfigurationRoot;
        public FleetManagerB2BApiConfiguration fleetManagerB2BConfig = new();
        public ESSApiConfiguration EssConfig = new();
        public FSSApiConfiguration FssConfig = new();
        public POSWebJobApiConfiguration POSWebJobConfig = new();
        public POSFileDetails posFileDetails = new();

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
            public string BatchStatusPollingCutoffTime { get; set; }
            public string BatchStatusPollingDelayTime { get; set; }
            public int BatchCommitWaitTime { get; set; }
        }
        public class POSWebJobApiConfiguration
        {
            public string UserName { get; set; }
            public string Password { get; set; }
            public string BaseUrl { get; set; }
            public bool IsRunningOnLocalMachine { get; set; }
        }

        public class POSFileDetails
        {
            public string M01IsoFile { get; set; }
            public string M02IsoFile { get; set; }
            public string M01Sha1File { get; set; }
            public string M02Sha1File { get; set; }
            public string ZipFilesBatchId { get; set; }
            public string IsoSha1BatchId { get; set; }
            public string UpdateExchangeSetBatchId { get; set; }
            public string MediaTypeDVD { get; set; }
            public string MediaTypeZip { get; set; }
            public string TempFolderName { get; set; }
            public string UpdateExchangeSet { get; set; }
        }

        public TestConfiguration()
        {
            ConfigurationRoot = new ConfigurationBuilder()
                               .AddJsonFile("appsettings.json", false)
                               .Build();

            ConfigurationRoot.Bind("FleetManagerB2BApiConfiguration", fleetManagerB2BConfig);
            ConfigurationRoot.Bind("ESSApiConfiguration", EssConfig);
            ConfigurationRoot.Bind("FSSApiConfiguration", FssConfig);
            ConfigurationRoot.Bind("POSWebjobApiConfiguration", POSWebJobConfig);
            ConfigurationRoot.Bind("POSFileDetails", posFileDetails);
        }
    }
}
