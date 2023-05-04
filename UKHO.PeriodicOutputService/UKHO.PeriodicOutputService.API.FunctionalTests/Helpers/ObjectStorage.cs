using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class ObjectStorage
    {
        protected string userCredentialsBytes;
        protected HttpResponseMessage unpResponse;
        protected GetUNPResponse getunp { get; set; }
        protected GetCatalogue getcat { get; set; }
        protected string EssJwtToken { get; set; }
        protected string FssJwtToken { get; set; }
        protected GetProductIdentifiers getproductIdentifier { get; set; }

        protected static TestConfiguration testConfiguration= new TestConfiguration();
        
        protected static POSWebJobApiConfiguration posWebJob = testConfiguration.POSWebJobConfig;
        protected static POSFileDetails posDetails = testConfiguration.posFileDetails;
        protected static FSSApiConfiguration FSSAuth = testConfiguration.FssConfig;    
        protected static ESSApiConfiguration ESSAuth = new TestConfiguration().EssConfig;
        protected readonly FleetManagerB2BApiConfiguration fleet = new TestConfiguration().fleetManagerB2BConfig;


        protected List<string> productIdentifiers = new();
        protected List<string> productIdentifiersAIO = new();
        protected List<string> DownloadedFolderPath;
    }
}

