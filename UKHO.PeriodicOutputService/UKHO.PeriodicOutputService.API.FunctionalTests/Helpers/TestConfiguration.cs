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
        }

        public class POSWebJobApiConfiguration
        {
            public string UserName { get; set; }
            public string Password { get; set; }
            public string BaseUrl { get; set; }
            public string MockApiBaseUrl { get; set; }
            public bool IsRunningOnLocalMachine { get; set; }
            public string FMConfigurationValidProductIdentifier { get; set; }
            public string FMConfigurationInValidProductIdentifier { get; set; }
            public string FMConfigurationFullAvcsPollingTimeOut { get; set; }
            public string FMConfigurationUpdatePollingTimeout { get; set; }
        }

        public class POSFileDetails
        {
            public string ZipFilesBatchId { get; set; }
            public string IsoSha1BatchId { get; set; }
            public string CatalogueBatchId { get; set; }
            public string UpdateExchangeSetBatchId { get; set; }
            public string InvalidProductIdentifierBatchId { get; set; }
            public string FullAvcsPollingTimeOutBatchId { get; set; }
            public string UpdatePollingTimeoutBatchId { get; set; }
            public string EncUpdateListCsvBatchId { get; set; }
            public string TempFolderName { get; set; }
            public string UpdateExchangeSet { get; set; }
            public string PosAvcsZipFileName { get; set; }
            public string PosAvcsIsoFileName { get; set; }
            public string PosAvcsIsoSha1FileName { get; set; }
            public string PosUpdateZipFileName { get; set; }
            public string AVCSCatalogueFileName { get; set; }
            public string EncUpdateListFileName { get; set; }
            public string AVCSCatalogueFileMimeType { get; set; }
            public string EncUpdateListFileMimeType { get; set; }
            public string IsoFileMimeType { get; set; }
            public string Sha1FileMimeType { get; set; }
            public string ZipFileMimeType { get; set; }
            public string PosDVDVolumeIdentifier { get; set; }
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
