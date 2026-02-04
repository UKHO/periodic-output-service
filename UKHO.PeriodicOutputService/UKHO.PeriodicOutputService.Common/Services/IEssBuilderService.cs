using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;

namespace UKHO.PeriodicOutputService.Common.Services
{
    public interface IEssBuilderService
    {
        Task<ExchangeSetResponseModel?> PostProductIdentifiersData(List<string> productIdentifiers, string? exchangeSetStandard = null, string? exchangeSetLayout = null, string? correlationId = null);

        Task<ExchangeSetResponseModel?> GetProductDataSinceDateTime(string sinceDateTime, string? exchangeSetStandard = null, string? exchangeSetLayout = null, string? correlationId = null);

        Task<ExchangeSetResponseModel?> GetProductDataProductVersions(ProductVersionsRequest productVersionsRequest, string? exchangeSetStandard = null, string? exchangeSetLayout = null, string? correlationId = null);
    }
}
