using System.Globalization;
using Microsoft.Extensions.Options;
using UKHO.FmEssFssMock.API.Common;
using UKHO.FmEssFssMock.API.Helpers;
using UKHO.FmEssFssMock.API.Models.Request;
using UKHO.FmEssFssMock.API.Models.Response;

namespace UKHO.FmEssFssMock.API.Services
{
    public class ExchangeSetService
    {
        private readonly IOptions<ExchangeSetServiceConfiguration> _essConfiguration;        
        private readonly FileShareService _fssService;
        protected IConfiguration _configuration;

        public ExchangeSetService(IOptions<ExchangeSetServiceConfiguration> essConfiguration,                                  
                                  FileShareService fssService,
                                  IConfiguration configuration)
        {
            _essConfiguration = essConfiguration;            
            _fssService = fssService;
            _configuration = configuration;
        }

        public ExchangeSetServiceResponse CreateExchangeSet(string[] productIdentifiers)
        {
            string productIdentifiersPattern = "productIdentifier-" + string.Join("-", productIdentifiers);
            CreateBatchRequest batchRequest = CreateBatchRequestModel();

            BatchResponse createBatchResponse = _fssService.CreateBatch(batchRequest.Attributes, _configuration["HOME"]);

            if (!string.IsNullOrEmpty(createBatchResponse.BatchId.ToString()))
            {
                string path = Path.Combine(Environment.CurrentDirectory, @"Data", createBatchResponse.BatchId.ToString());
                foreach (var fileName in Directory.GetFiles(path))
                {
                    FileInfo file = new FileInfo(fileName);

                    bool isFileAdded = _fssService.AddFile(createBatchResponse.BatchId.ToString(), file.Name, _configuration["HOME"]);

                    if (!isFileAdded)
                    {
                        return null;
                    }
                }
                return GetProductIdentifier(productIdentifiersPattern);
            }
            return null;
        }

        private ExchangeSetServiceResponse GetProductIdentifier(string productIdentifiers)
        {
            List<ExchangeSetServiceResponse>? responseData = FileHelper.ReadJsonFile<List<ExchangeSetServiceResponse>>(_essConfiguration.Value.EssDataDirectoryPath + _essConfiguration.Value.PostProductIdentifiersResponseFileName);
            ExchangeSetServiceResponse? selectedProductIdentifier = responseData?.FirstOrDefault(a => a.Id.ToLowerInvariant() == productIdentifiers.ToLowerInvariant());
            selectedProductIdentifier.ResponseBody.ExchangeSetUrlExpiryDateTime = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            return selectedProductIdentifier;
        }

        private CreateBatchRequest CreateBatchRequestModel()
        {
            CreateBatchRequest createBatchRequest = new()
            {
                BusinessUnit = "AVCSCustomExchangeSets",
                Attributes = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("Exchange Set Type", "Update"),
                    new KeyValuePair<string, string>("Media Type", "Zip"),
                    new KeyValuePair<string, string>("Product Type", "AVCS")
                },
                ExpiryDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                Acl = new Acl()
                {
                    ReadUsers = new List<string>() { "public" }
                }
            };

            return createBatchRequest;
        }
    }
}
