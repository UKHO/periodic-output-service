using Microsoft.Extensions.Options;
using UKHO.FmEssFssMock.API.Common;
using UKHO.FmEssFssMock.API.Helper;
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

        public ExchangeSetServiceResponse GetProductIdentifier()
        {
            var responseData = FileHelper.ReadJsonFile<ExchangeSetServiceResponse>(_exchangeSetConfiguration.Value.FileDirectoryPath + _exchangeSetConfiguration.Value.EssResponseFile);
            ////var selectedProductIdentifier = responseData?.FirstOrDefault(a => a.Id.ToLowerInvariant() == productIdentifiers.ToLowerInvariant());
            return responseData;
        }
    }
}
