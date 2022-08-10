using System.Globalization;
using Microsoft.Extensions.Options;
using UKHO.FmEssFssMock.API.Common;
using UKHO.FmEssFssMock.API.Helpers;
using UKHO.FmEssFssMock.API.Models.Response;

namespace UKHO.FmEssFssMock.API.Services
{
    public class ExchangeSetService
    {
        private readonly IOptions<ExchangeSetConfiguration> _exchangeSetConfiguration;

        public ExchangeSetService(IOptions<ExchangeSetConfiguration> exchangeSetConfiguration)
        {
            _exchangeSetConfiguration = exchangeSetConfiguration;
        }

        public ExchangeSetServiceResponse GetProductIdentifier(string productIdentifiers)
        {
            List<ExchangeSetServiceResponse>? responseData = FileHelper.ReadJsonFile<List<ExchangeSetServiceResponse>>(_exchangeSetConfiguration.Value.FileDirectoryPath + _exchangeSetConfiguration.Value.EssResponseFile);
            ExchangeSetServiceResponse? selectedProductIdentifier = responseData?.FirstOrDefault(a => a.Id.ToLowerInvariant() == productIdentifiers.ToLowerInvariant());
            selectedProductIdentifier.ResponseBody.ExchangeSetUrlExpiryDateTime = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            return selectedProductIdentifier;
        }
    }
}
