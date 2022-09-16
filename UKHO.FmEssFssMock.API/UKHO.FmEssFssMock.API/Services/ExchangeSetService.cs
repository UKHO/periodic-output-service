using System.Globalization;
using Microsoft.Extensions.Options;
using UKHO.FmEssFssMock.API.Common;
using UKHO.FmEssFssMock.API.Helpers;
using UKHO.FmEssFssMock.API.Models.Request;
using UKHO.FmEssFssMock.API.Models.Response;
using UKHO.FmEssFssMock.Enums;

namespace UKHO.FmEssFssMock.API.Services
{
    public class ExchangeSetService
    {
        private readonly IOptions<ExchangeSetServiceConfiguration> _essConfiguration;
        private readonly FileShareService _fssService;
        protected IConfiguration _configuration;
        private readonly string _homeDirectoryPath = string.Empty;
        private readonly MockService _mockService;
        public ExchangeSetService(IOptions<ExchangeSetServiceConfiguration> essConfiguration,
                                  FileShareService fssService,
                                  IConfiguration configuration,
                                  MockService mockService)
        {
            _essConfiguration = essConfiguration;
            _fssService = fssService;
            _configuration = configuration;
            _mockService = mockService;

            _homeDirectoryPath = Path.Combine(_configuration["HOME"], _configuration["POSFolderName"]);
        }

        public ExchangeSetServiceResponse CreateExchangeSetForGetProductDataSinceDateTime(string sinceDateTime)
        {
            CreateBatchRequest batchRequest = CreateBatchRequestModel(false);

            BatchResponse createBatchResponse = _fssService.CreateBatch(batchRequest.Attributes, _homeDirectoryPath);

            if (!string.IsNullOrEmpty(createBatchResponse.BatchId.ToString()))
            {
                string path = Path.Combine(Environment.CurrentDirectory, @"Data", createBatchResponse.BatchId.ToString());
                foreach (string fileName in Directory.GetFiles(path))
                {
                    FileInfo file = new(fileName);

                    bool isFileAdded = _fssService.AddFile(createBatchResponse.BatchId.ToString(), file.Name, _homeDirectoryPath);

                    if (!isFileAdded)
                    {
                        return null;
                    }
                }
                return GetEssResponse("sincedatetime");
            }
            return null;
        }


        public ExchangeSetServiceResponse CreateExchangeSetForPostProductIdentifier(string[] productIdentifiers)
        {
            string productIdentifiersPattern = "productIdentifier-" + string.Join("-", productIdentifiers);
            CreateBatchRequest batchRequest = CreateBatchRequestModel(true);

            BatchResponse createBatchResponse = _fssService.CreateBatch(batchRequest.Attributes, _homeDirectoryPath);

            if (!string.IsNullOrEmpty(createBatchResponse.BatchId.ToString()))
            {
                string path = Path.Combine(Environment.CurrentDirectory, @"Data", createBatchResponse.BatchId.ToString());
                foreach (string fileName in Directory.GetFiles(path))
                {
                    FileInfo file = new(fileName);

                    bool isFileAdded = _fssService.AddFile(createBatchResponse.BatchId.ToString(), file.Name, _homeDirectoryPath);

                    if (!isFileAdded)
                    {
                        return null;
                    }
                }
                return GetEssResponse(productIdentifiersPattern);
            }
            return null;
        }


        private ExchangeSetServiceResponse GetEssResponse(string responseId)
        {
            List<ExchangeSetServiceResponse>? responseData = FileHelper.ReadJsonFile<List<ExchangeSetServiceResponse>>(_essConfiguration.Value.EssDataDirectoryPath + _essConfiguration.Value.PostProductIdentifiersResponseFileName);
            ExchangeSetServiceResponse? selectedProductIdentifier = responseData?.FirstOrDefault(a => a.Id.ToLowerInvariant() == responseId.ToLowerInvariant());
            selectedProductIdentifier.ResponseBody.ExchangeSetUrlExpiryDateTime = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            return selectedProductIdentifier;
        }

        private CreateBatchRequest CreateBatchRequestModel(bool isPostProductIdentifiersRequest)
        {
            PosTestCase currentTestCase = _mockService.GetCurrentPOSTestCase(_homeDirectoryPath);

            CreateBatchRequest createBatchRequest = new()
            {
                BusinessUnit = "AVCSCustomExchangeSets",
                Attributes = new List<KeyValuePair<string, string>>()
                {
                    new("Exchange Set Type", "Update"),
                    new("Media Type", "Zip"),

                },
                ExpiryDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                Acl = new Acl()
                {
                    ReadUsers = new List<string>() { "public" }
                }
            };

            if (currentTestCase != PosTestCase.ValidProductIdentifiers)
            {
                createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Batch Type", currentTestCase.ToString()));
            }
            else
            {
                createBatchRequest.Attributes.Add(new KeyValuePair<string, string>("Batch Type", isPostProductIdentifiersRequest ? Batch.EssFullAvcsZipBatch.ToString() : Batch.EssUpdateZipBatch.ToString()));
            }
            return createBatchRequest;
        }
    }
}
