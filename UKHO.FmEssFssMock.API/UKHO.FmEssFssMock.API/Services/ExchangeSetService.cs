using System.Globalization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.FmEssFssMock.API.Common;
using UKHO.FmEssFssMock.API.Controllers;
using UKHO.FmEssFssMock.API.Helpers;
using UKHO.FmEssFssMock.API.Models.Request;
using UKHO.FmEssFssMock.API.Models.Response;
using UKHO.PeriodicOutputService.Common.Models.Request;

namespace UKHO.FmEssFssMock.API.Services
{
    public class ExchangeSetService
    {
        private readonly IOptions<ExchangeSetServiceConfiguration> _essConfiguration;
        private readonly FileShareServiceController _fssController;

        public ExchangeSetService(IOptions<ExchangeSetServiceConfiguration> essConfiguration,
                                  FileShareServiceController fssController)
        {
            _essConfiguration = essConfiguration;
            _fssController = fssController;
        }

        public ExchangeSetServiceResponse CreateExchangeSet(string[] productIdentifiers)
        {
            string productIdentifiersPattern = "productIdentifier-" + string.Join("-", productIdentifiers);
            CreateBatchRequest batchRequest = CreateBatchRequestModel();

            var result = _fssController.CreateBatch(batchRequest);

            if (true)
            {
                string path = Path.Combine(Environment.CurrentDirectory, @"Data", "2270F318-639C-4E64-A0C0-CADDD5F4EB05");
                foreach (var fileName in Directory.GetFiles(path))
                {
                    FileInfo file = new FileInfo(fileName);

                    var addFileResponse = _fssController.AddFileToBatch("2270F318-639C-4E64-A0C0-CADDD5F4EB05", file.Name, "application/octet-stream", file.Length, new FileRequest());

                    if (false)
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
            List<ExchangeSetServiceResponse>? responseData = FileHelper.ReadJsonFile<List<ExchangeSetServiceResponse>>(_essConfiguration.Value.FileDirectoryPath + _essConfiguration.Value.EssResponseFile);
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

        private AddFileRequest CreateAddFileRequestModel()
        {
            AddFileRequest addFileToBatchRequestModel = new()
            {
                Attributes = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("Exchange Set Type", "Base"),
                    new KeyValuePair<string, string>("Media Type", "DVD"),
                    new KeyValuePair<string, string>("Product Type", "AVCS")
                }
            };
            return addFileToBatchRequestModel;
        }
    }
}
