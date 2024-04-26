using System.Globalization;
using Microsoft.Extensions.Options;
using UKHO.FmEssFssMock.API.Common;
using UKHO.FmEssFssMock.API.Enums;
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

        public ExchangeSetServiceResponse CreateExchangeSetForGetProductDataSinceDateTime(string sinceDateTime, string exchangeSetStandard)
        {
            CreateBatchRequest batchRequest = CreateBatchRequestModel(EssEndPoints.ProductDataSinceDateTime, exchangeSetStandard);

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


        public ExchangeSetServiceResponse CreateExchangeSetForPostProductIdentifier(string[] productIdentifiers, string exchangeSetStandard)
        {
            string productIdentifiersPattern = "productIdentifier-" + string.Join("-", productIdentifiers);
            productIdentifiersPattern += !string.IsNullOrEmpty(exchangeSetStandard) ? "-" + exchangeSetStandard : "";
            CreateBatchRequest batchRequest;

            if (productIdentifiers.Contains("GB800001") ||
                productIdentifiers.Contains("GA800001") ||
                productIdentifiers.Contains("GC800001"))
            {
                batchRequest = CreateBatchRequestModelForAIO(true);
            }
            else
            {
                batchRequest = CreateBatchRequestModel(EssEndPoints.ProductIdentifiers, exchangeSetStandard);
            }


            BatchResponse createBatchResponse = _fssService.CreateBatch(batchRequest.Attributes, _homeDirectoryPath);

            if (!string.IsNullOrEmpty(createBatchResponse.BatchId.ToString()))
            {
                string path = Path.Combine(Environment.CurrentDirectory, @"Data", createBatchResponse.BatchId.ToString());
                foreach (string filePath in Directory.GetFiles(path))
                {
                    FileInfo file = new(filePath);

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

        public ExchangeSetServiceResponse CreateExchangeSetForPostProductVersion(List<ProductVersionRequest> productVersionsRequest, string exchangeSetStandard)
        {
            CreateBatchRequest batchRequest;

            foreach (ProductVersionRequest item in productVersionsRequest)
            {
                if (item.ProductName.Contains("GB800001") ||
                    item.ProductName.Contains("GA800001") ||
                    item.ProductName.Contains("GC800001"))
                {
                    batchRequest = CreateBatchRequestModelForAIO(false);
                }
                else
                {
                    batchRequest = CreateBatchRequestModel(EssEndPoints.PostProductVersion, exchangeSetStandard);
                }

                // this will set attributes for empty ess
                //if (item.EditionNumber > 0)
                //{
                //    batchRequest.Attributes = new List<KeyValuePair<string, string>>()
                //    {
                //        new("Batch Type", Batch.EssEmptyBatch.ToString())
                //    };
                //}

                BatchResponse createBatchResponse = _fssService.CreateBatch(batchRequest.Attributes, _homeDirectoryPath);
                string productVersion = $"productVersion";
                if (productVersionsRequest.Count > 1)
                {
                    foreach (ProductVersionRequest items in productVersionsRequest)
                    {
                        productVersion += $"-{items.ProductName}-{items.EditionNumber}-{items.UpdateNumber}";
                    }
                    productVersion += !string.IsNullOrEmpty(exchangeSetStandard) ? "-" + exchangeSetStandard : "";
                }
                else
                {
                    productVersion += !string.IsNullOrEmpty(exchangeSetStandard) ? $"-{item.ProductName}-{item.EditionNumber}-{item.UpdateNumber}-{exchangeSetStandard}" : $"-{item.ProductName}-*-*";
                }
                if (!string.IsNullOrEmpty(createBatchResponse.BatchId.ToString()))
                {
                    string path = Path.Combine(Environment.CurrentDirectory, @"Data", createBatchResponse.BatchId.ToString());
                    foreach (string filePath in Directory.GetFiles(path))
                    {
                        FileInfo file = new(filePath);
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

        private CreateBatchRequest CreateBatchRequestModel(EssEndPoints essEndPoints, string exchangeSetStandard)
        {
            PosTestCase currentTestCase = _mockService.GetCurrentPOSTestCase(_homeDirectoryPath);
            string batchType;

            if (currentTestCase == PosTestCase.ValidProductIdentifiers)
            {
                if (essEndPoints.Equals(EssEndPoints.ProductIdentifiers))
                {
                    if (exchangeSetStandard == "s63")
                        batchType = Batch.EssProductIdentifiersS63ZipBatch.ToString();

                    else if (exchangeSetStandard == "s57")
                        batchType = Batch.EssProductIdentifiersS57ZipBatch.ToString();

                    else
                        batchType = Batch.EssFullAvcsZipBatch.ToString();
                }
                else if (essEndPoints.Equals(EssEndPoints.PostProductVersion))
                {
                    if (exchangeSetStandard == "s63")
                        batchType = Batch.EssPostProductVersionS63ZipBatch.ToString();

                    else if (exchangeSetStandard == "s57")
                        batchType = Batch.EssPostProductVersionS57ZipBatch.ToString();
                    else
                        batchType = Batch.EssZipBatch.ToString();
                }
                else //since datetime
                {
                    batchType = Batch.EssUpdateZipBatch.ToString();
                }
            }
            else
            {
                batchType = currentTestCase.ToString();
            }

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
            string batchType = string.Empty;

            if (currentTestCase == AioTestCase.ValidAioProductIdentifier)
            {
                if (isPostProductIdentifiersRequest)
                    batchType = Batch.EssAioBaseZipBatch.ToString();
                else
                    batchType = Batch.EssAioUpdateZipBatch.ToString();
            }
            else
            {
                batchType = currentTestCase.ToString();
            }


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
