﻿using Microsoft.Extensions.Configuration;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class TestConfiguration
    {
        protected IConfigurationRoot configurationRoot;
        public string? AzureWebJobsStorage;
        public BessApiConfiguration bessConfig = new();
        public SharedKeyConfiguration sharedKeyConfig = new();
        public ScsApiConfiguration scsConfig = new();
        public EssApiConfiguration authTokenConfig = new();
        public FssApiConfiguration fssConfig = new();
        public BessStorageConfiguration bessStorageConfig = new();
        public ExchangeSetDetails exchangeSetDetails = new();
        public PksApiConfiguration pksConfig = new();

        public class BessApiConfiguration
        {
            public string? BaseUrl { get; set; }
            public string? ValidConfigPath { get; set; }
            public string? InvalidConfigPath { get; set; }
            public string? s63ExchangeSetStandard { get; set; }
            public string? s57ExchangeSetStandard { get; set; }
            public string? TempFolderName { get; set; }
            public List<string>? ProductsName { get; set; }
            public List<string>? EditionNumber { get; set; }
            public string? Identifiers { get; set; }
            public List<string>? Keys { get; set; }
            public List<string>? Permits { get; set; }
            public string? KeyTxtFile { get; set; }
            public string? PermitXmlFile { get; set; }
            public string? PermitDate { get; set; }
            public List<string>? BessBatchDetails { get; set; }
        }

        public class SharedKeyConfiguration
        {
            public string? Key { get; set; }
        }

        public class ScsApiConfiguration
        {
            public string? BaseUrl { get; set; }
        }

        public class EssApiConfiguration
        {
            public string? BaseUrl { get; set; }
            public string? ProductName { get; set; }
            public int? EditionNumber { get; set; }
            public int? UpdateNumber { get; set; }
        }

        public class FssApiConfiguration
        {
            public string? BaseUrl { get; set; }
            public int BatchCommitWaitTime { get; set; }
        }

        public class BessStorageConfiguration
        {
            public string? ConnectionString { get; set; }
            public string? ContainerName { get; set; }
            public string? TableName { get; set; }
            public string? QueueName { get; set; }
            public string? MessageContainerName { get; set; }
        }

        public class ExchangeSetDetails
        {
            public string? ExchangeSetFileName { get; set; }
            public string? ExchangeReadMeFile { get; set; }
            public string? ExchangeSetProductFile { get; set; }
            public string? ExchangeSetProductFilePath { get; set; }
            public string? ExchangeSetEncRootFolder { get; set; }
            public string? ExchangeSetCatalogueFile { get; set; }
            public string? ExchangeSetSerialEncFile { get; set; }
            public string? BessExchangeSetFileName { get; set; }
        }

        public class PksApiConfiguration
        {
            public string? BaseUrl { get; set; }
        }

        public TestConfiguration()
        {
            configurationRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .Build();
            AzureWebJobsStorage = configurationRoot.GetSection("AzureWebJobsStorage").Value;
            configurationRoot.Bind("BESSApiConfiguration", bessConfig);
            configurationRoot.Bind("SharedKeyConfiguration", sharedKeyConfig);
            configurationRoot.Bind("SCSApiConfiguration", scsConfig);
            configurationRoot.Bind("ESSApiConfiguration", authTokenConfig);
            configurationRoot.Bind("FSSApiConfiguration", fssConfig);
            configurationRoot.Bind("BessStorageConfiguration", bessStorageConfig);
            configurationRoot.Bind("ExchangeSetDetails", exchangeSetDetails);
            configurationRoot.Bind("PKSApiConfiguration", pksConfig);
        }
    }
}
