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
        private readonly string _homeDirectoryPath;
        private readonly MockService _mockService;

        public ExchangeSetService(IOptions<ExchangeSetServiceConfiguration> essConfiguration,
                                  FileShareService fssService,
                                  IConfiguration configuration,
                                  MockService mockService)
        {
            _essConfiguration = essConfiguration;
            _fssService = fssService;
            _mockService = mockService;

            _homeDirectoryPath = Path.Combine(configuration["HOME"], configuration["POSFolderName"]);
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
            CreateBatchRequest batchRequest;

            if (productIdentifiers.Contains("GB800001"))
            {
                batchRequest = CreateBatchRequestModelForAIO(true);
            }
            else
            {
                batchRequest = CreateBatchRequestModel(true);
            }


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

        public ExchangeSetServiceResponse CreateExchangeSetForPostProductVersion(List<ProductVersionRequest> productVersionsRequest)
        {
            CreateBatchRequest batchRequest;

            foreach (ProductVersionRequest? item in productVersionsRequest)
            {
                if(item.ProductName.Contains("GB800001"))
                {
                    batchRequest = CreateBatchRequestModelForAIO(false);
                }
                else
                {
                    batchRequest = CreateBatchRequestModel(true);
                }

                BatchResponse createBatchResponse = _fssService.CreateBatch(batchRequest.Attributes, _homeDirectoryPath);
                string productVersion = $"productVersion-{item.ProductName}-{item.EditionNumber}-{item.UpdateNumber}";
        
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
                    return GetEssResponse(productVersion);
                }     
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
            string batchType;

            batchType = currentTestCase != PosTestCase.ValidProductIdentifiers
               ? currentTestCase.ToString()
               : isPostProductIdentifiersRequest ? Batch.EssFullAvcsZipBatch.ToString() : Batch.EssUpdateZipBatch.ToString();

            CreateBatchRequest createBatchRequest = new()
            {
                BusinessUnit = "AVCSCustomExchangeSets",
                Attributes = new List<KeyValuePair<string, string>>()
                {
                    new("Exchange Set Type", "Update"),
                    new("Media Type", "Zip"),
                    new("Batch Type", batchType)
                },
                ExpiryDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                Acl = new Acl()
                {
                    ReadUsers = new List<string>() { "public" }
                }
            };

            return createBatchRequest;
        }

        private CreateBatchRequest CreateBatchRequestModelForAIO(bool isPostProductIdentifiersRequest)
        {
            AioTestCase currentTestCase = _mockService.GetCurrentAIOTestCase(_homeDirectoryPath);
            string batchType;

            batchType = currentTestCase != AioTestCase.ValidAioProductIdentifier
                  ? currentTestCase.ToString()
                  : isPostProductIdentifiersRequest ? Batch.ValidAioProductIdentifier.ToString() : Batch.AioUpdateZipBatch.ToString();
           
           
                CreateBatchRequest createBatchRequest = new()
                {
                    BusinessUnit = "AVCSCustomExchangeSets",
                    Attributes = new List<KeyValuePair<string, string>>()
                {
                    new("Exchange Set Type", "Update"),
                    new("Media Type", "Zip"),
                    new("Batch Type", batchType)
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
