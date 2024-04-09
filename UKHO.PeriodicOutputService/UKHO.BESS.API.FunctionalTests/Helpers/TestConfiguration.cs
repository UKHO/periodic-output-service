using Microsoft.Extensions.Configuration;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class TestConfiguration
    {
        protected IConfigurationRoot configurationRoot;
        public string EssBaseAddress;
        public static string FakeTokenPrivateKey;
        public string ExchangeSetFileName;
        public string ExchangeSetSerialEncFile;
        public string ExchangeReadMeFile;
        public string EssStorageAccountConnectionString;
        public string ExchangeSetProductFile;
        public string ExchangeSetProductFilePath;
        public string ExchangeSetProductType;
        public string ExchangeSetCatalogueType;
        public string ExchangeSetEncRootFolder;
        public string ExchangeSetCatalogueFile;
        public string AzureWebJobsStorage;


        public BessApiConfiguration bessConfig = new();
        public SharedKeyConfiguration sharedKeyConfig = new();
        public SCSApiConfiguration scsConfig = new();
        public ESSApiConfiguration authTokenConfig = new();
        public FSSApiConfiguration fssConfig = new();
        public BessStorageConfiguration bessStorageConfig = new();

        public class BessApiConfiguration
        {
            public string? BaseUrl { get; set; }
            public string? ValidConfigPath { get; set; }
            public string? InvalidConfigPath { get; set; }
            public string? s63ExchangeSetStandard { get; set; }
            public string? s57ExchangeSetStandard { get; set; }
            public string? TempFolderName { get; set; }
            public List<string>? ProductsName { get; set; }
        }

        public class SharedKeyConfiguration
        {
            public string? Key { get; set; }
        }

        public class SCSApiConfiguration
        {
            public string? BaseUrl { get; set; }
        }

        public class ESSApiConfiguration
        {
            public string? BaseUrl { get; set; }
            public string? ProductName { get; set; }
            public int? EditionNumber { get; set; }
            public int? UpdateNumber { get; set; }
        }

        public class FSSApiConfiguration    
        {
            public string? BaseUrl { get; set; }
            public int BatchCommitWaitTime { get; set; }
        }

        public class BessStorageConfiguration
        {
            public string? QueueName { get; set; }
        }

        public TestConfiguration()
        {
            configurationRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .Build();

            EssStorageAccountConnectionString = configurationRoot.GetSection("EssStorageAccountConnectionString").Value;
            EssBaseAddress = configurationRoot.GetSection("EssApiUrl").Value;
            ExchangeSetFileName = configurationRoot.GetSection("ExchangeSetFileName").Value;
            ExchangeSetSerialEncFile = configurationRoot.GetSection("ExchangeSetSerialEncFile").Value;
            ExchangeReadMeFile = configurationRoot.GetSection("ExchangeReadMeFile").Value;
            FakeTokenPrivateKey = configurationRoot.GetSection("FakeTokenPrivateKey").Value;
            ExchangeSetProductFile = configurationRoot.GetSection("ExchangeSetProductFile").Value;
            ExchangeSetProductFilePath = configurationRoot.GetSection("ExchangeSetProductFilePath").Value;
            ExchangeSetProductType = configurationRoot.GetSection("ExchangeSetProductType").Value;
            ExchangeSetCatalogueType = configurationRoot.GetSection("ExchangeSetCatalogueType").Value;
            ExchangeSetEncRootFolder = configurationRoot.GetSection("ExchangeSetEncRootFolder").Value;
            ExchangeSetCatalogueFile = configurationRoot.GetSection("ExchangeSetCatalogueFile").Value;
            AzureWebJobsStorage = configurationRoot.GetSection("AzureWebJobsStorage").Value;
            configurationRoot.Bind("BESSApiConfiguration", bessConfig);
            configurationRoot.Bind("SharedKeyConfiguration", sharedKeyConfig);
            configurationRoot.Bind("SCSApiConfiguration", scsConfig);
            configurationRoot.Bind("ESSApiConfiguration", authTokenConfig);
            configurationRoot.Bind("FSSApiConfiguration", fssConfig);
            configurationRoot.Bind("BessStorageConfiguration", bessStorageConfig);
        }
    }
}
